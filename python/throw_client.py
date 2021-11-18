import math
import os.path
import time
import glob
import cv2
import numpy as np
import throw
import sys

client = throw.ThrowClient(sys.argv[1], int(sys.argv[2]))

image = cv2.imread(sys.argv[3])

cv2.namedWindow("image", cv2.WINDOW_NORMAL)

while True:
    print("tick", image.shape)
    header, response_image = client.send_message("sample_command", image)
    print(
        "receivede",
        header,
        response_image.shape,
        np.min(response_image),
        np.max(response_image),
    )
    response_image = response_image * 100
    cv2.imshow("image", response_image)
    cv2.waitKey(0)
    # time.sleep(1)
