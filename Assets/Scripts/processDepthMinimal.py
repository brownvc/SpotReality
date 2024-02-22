import sys
import numpy as np
import cv2

FULL_KERNEL_5 = np.ones((5, 5), np.uint8)
FULL_KERNEL_9 = np.ones((9, 9), np.uint8)

# 3x3 cross kernel
CROSS_KERNEL_3 = np.asarray(
    [
        [0, 1, 0],
        [1, 1, 1],
        [0, 1, 0],
    ], dtype=np.uint8)

# 5x5 cross kernel
CROSS_KERNEL_5 = np.asarray(
    [
        [0, 0, 1, 0, 0],
        [0, 0, 1, 0, 0],
        [1, 1, 1, 1, 1],
        [0, 0, 1, 0, 0],
        [0, 0, 1, 0, 0],
    ], dtype=np.uint8)

# 7x7 cross kernel
CROSS_KERNEL_7 = np.asarray(
    [
        [0, 0, 0, 1, 0, 0, 0],
        [0, 0, 0, 1, 0, 0, 0],
        [0, 0, 0, 1, 0, 0, 0],
        [1, 1, 1, 1, 1, 1, 1],
        [0, 0, 0, 1, 0, 0, 0],
        [0, 0, 0, 1, 0, 0, 0],
        [0, 0, 0, 1, 0, 0, 0],
    ], dtype=np.uint8)



def fill_in_multiscale(depth_map, max_depth=100.0,
                       dilation_kernel_far=CROSS_KERNEL_3,
                       dilation_kernel_med=CROSS_KERNEL_5,
                       dilation_kernel_near=CROSS_KERNEL_7):

    # Convert to float32
    depths_in = np.float32(depth_map)

    # Calculate bin masks before inversion
    valid_pixels_near = (depths_in > 0.1) & (depths_in <= 15.0)
    valid_pixels_med = (depths_in > 15.0)

    # Invert (and offset)
    s1_inverted_depths = np.copy(depths_in)
    valid_pixels = (s1_inverted_depths > 0.1)
    s1_inverted_depths[valid_pixels] = \
        max_depth - s1_inverted_depths[valid_pixels]

    # Multi-scale dilation
    dilated_med = cv2.dilate(
        np.multiply(s1_inverted_depths, valid_pixels_med),
        dilation_kernel_med)
    dilated_near = cv2.dilate(
        np.multiply(s1_inverted_depths, valid_pixels_near),
        dilation_kernel_near)

    # Find valid pixels for each binned dilation
    valid_pixels_near = (dilated_near > 0.1)
    valid_pixels_med = (dilated_med > 0.1)

    # Combine dilated versions, starting farthest to nearest
    s2_dilated_depths = np.copy(s1_inverted_depths)
    s2_dilated_depths[valid_pixels_med] = dilated_med[valid_pixels_med]
    s2_dilated_depths[valid_pixels_near] = dilated_near[valid_pixels_near]

    # Small hole closure
    s3_closed_depths = cv2.morphologyEx(
        s2_dilated_depths, cv2.MORPH_CLOSE, FULL_KERNEL_5)

    # Median blur to remove outliers
    s4_blurred_depths = np.copy(s3_closed_depths)
    blurred = cv2.medianBlur(s3_closed_depths, 5)
    valid_pixels = (s3_closed_depths > 0.1)
    s4_blurred_depths[valid_pixels] = blurred[valid_pixels]

    # Calculate a top mask
    top_mask = np.ones(depths_in.shape, dtype=bool)
    for pixel_col_idx in range(s4_blurred_depths.shape[1]):
        pixel_col = s4_blurred_depths[:, pixel_col_idx]
        top_pixel_row = np.argmax(pixel_col > 0.1)
        top_mask[0:top_pixel_row, pixel_col_idx] = False

    # Get empty mask
    valid_pixels = (s4_blurred_depths > 0.1)
    empty_pixels = ~valid_pixels & top_mask

    # Hole fill
    dilated = cv2.dilate(s4_blurred_depths, FULL_KERNEL_9)
    s5_dilated_depths = np.copy(s4_blurred_depths)
    s5_dilated_depths[empty_pixels] = dilated[empty_pixels]

    # Fill large holes with masked dilations
    s7_blurred_depths = np.copy(s5_dilated_depths)
    for i in range(6):
        empty_pixels = (s7_blurred_depths < 0.1) & top_mask
        dilated = cv2.dilate(s7_blurred_depths, FULL_KERNEL_5)
        s7_blurred_depths[empty_pixels] = dilated[empty_pixels]

    # Median blur
    blurred = cv2.medianBlur(s7_blurred_depths, 5)
    valid_pixels = (s7_blurred_depths > 0.1) & top_mask
    s7_blurred_depths[valid_pixels] = blurred[valid_pixels]

    blurred = cv2.bilateralFilter(s7_blurred_depths, 5, 0.5, 2.0)
    s7_blurred_depths[valid_pixels] = blurred[valid_pixels]

    # Invert (and offset)
    s8_inverted_depths = np.copy(s7_blurred_depths)
    valid_pixels = np.where(s8_inverted_depths > 0.1)
    s8_inverted_depths[valid_pixels] = \
        max_depth - s8_inverted_depths[valid_pixels]

    depths_out = s8_inverted_depths

    return depths_out


def main():
    depth_image_path = sys.argv[1]

    with open(depth_image_path, 'rb') as file:
        num_elements = np.fromfile(file, dtype=np.int32, count=1)
        data = np.fromfile(file, dtype=np.float32, count=int(num_elements))

    projected_depths = data.reshape((480, 640))

    final_depths= fill_in_multiscale(projected_depths)

    length = 480 * 640
    file_path = depth_image_path + "_preprocess"
    with open(file_path, 'wb') as file:
        file.write(length.to_bytes(4, byteorder='little', signed=True))
        final_depths.tofile(file)

if __name__ == '__main__':
    main()