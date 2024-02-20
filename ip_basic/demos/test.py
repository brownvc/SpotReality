import glob
import os
import sys
import time

import cv2
import numpy as np
import png

import sys
import os

import depth_map_utils
import vis_utils

fill_type = 'multiscale'
extrapolate = False
blur_type = 'bilateral'

show_process = True
save_depth_maps = False

depth_image_path = "./mesh_array_0";
# depth_image = cv2.imread(depth_image_path, cv2.IMREAD_ANYDEPTH)
# projected_depths = np.float32(depth_image)

with open(depth_image_path, 'rb') as file:
    # Assuming the first 4 bytes represent an int32 indicating the length
    num_elements = np.fromfile(file, dtype=np.int32, count=1)
    # Now read the floating-point numbers
    data = np.fromfile(file, dtype=np.float32, count=int(num_elements))

projected_depths = data.reshape((480, 640))

print("start")

final_depths, process_dict = depth_map_utils.fill_in_multiscale(
                projected_depths, extrapolate=extrapolate, blur_type=blur_type,
                show_process=show_process)

print("ok")

img_size = (640, 480)

x_start = 80
y_start = 50
x_offset = img_size[0]
y_offset = img_size[1]
x_padding = 0
y_padding = 28

img_x = x_start
img_y = y_start
max_x = 1900

row_idx = 0
for key, value in process_dict.items():

    image_jet = cv2.applyColorMap(
        np.uint8(value / np.amax(value) * 255),
        cv2.COLORMAP_JET)

    img_x += x_offset + x_padding
    if (img_x + x_offset + x_padding) > max_x:
        img_x = x_start
        row_idx += 1
    img_y = y_start + row_idx * (y_offset + y_padding)

    # Save process images
    cv2.imwrite('./process/' + key + '.png', image_jet)

length = 480 * 640
file_path = './mesh_array_ip_process'  # Adjust path as needed
with open(file_path, 'wb') as file:
    # Write the length of the tensor as a 32-bit integer
    file.write(length.to_bytes(4, byteorder='little', signed=True))
    # Write the tensor elements as 32-bit floats
    final_depths.tofile(file)

image_jet = cv2.applyColorMap(
        np.uint8(final_depths / np.amax(value) * 255),
        cv2.COLORMAP_JET)
cv2.imwrite('./process/recheck.png', image_jet)

