import sys

sys.path.append("../../../python")
import numpy as np
import throw
import sys
import time
import numpy as np
import cv2


def convert_multichannel_depth(depth, far=20.0, near=0.5):
    low = depth[:, :, 0] / 255.0
    high = depth[:, :, 1] / 255.0
    d = depth[:, :, 2] / 255.0
    dd = far * (high + low / 256.0) + d * far / 256.0
    return dd


client = throw.ThrowClient(sys.argv[1], int(sys.argv[2]))

while True:

    # Send data
    header, tensor = client.send_message("sample_command", tensor=None)

    print(tensor.min(), tensor.max(), tensor.dtype)

    depth = 1 / convert_multichannel_depth(tensor)
    depth = cv2.normalize(
        depth, None, alpha=0, beta=255, norm_type=cv2.NORM_MINMAX, dtype=cv2.CV_8UC1
    )
    depth = cv2.applyColorMap(depth, cv2.COLORMAP_MAGMA)
    cv2.imshow("depth", depth)
    # cv2.imshow("image", cv2.cvtColor(tensor.astype(np.uint8), cv2.COLOR_RGB2BGR))
    cv2.waitKey(1)
