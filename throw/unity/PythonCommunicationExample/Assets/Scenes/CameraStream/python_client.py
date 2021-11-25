import sys

sys.path.append("../../../python")
import numpy as np
import throw
import sys
import time
import numpy as np
import cv2


def convert_multichannel_depth(
    depth: np.ndarray, far: float = 20.0, near: float = 0.5
) -> np.ndarray:
    """Converts a 3-channel depth image from the Unity Camera system.

    :param depth: unity depth [values are relative to the camera clip planes]
    :type depth: np.ndarray
    :param far: unity camera far plane, defaults to 20.0
    :type far: float, optional
    :param near: unity camera near plane , defaults to 0.5
    :type near: float, optional
    :return: depth in [meters] (or Unity units)
    :rtype: np.ndarray
    """
    low = depth[:, :, 0] / 255.0
    high = depth[:, :, 1] / 255.0
    d = depth[:, :, 2] / 255.0
    dd = far * (high + low / 256.0) + d * far / 256.0
    return dd


# Throw Client
client = throw.ThrowClient(sys.argv[1], int(sys.argv[2]))

while True:

    # Request for depth
    header, depth_tensor = client.send_message("get:depth", tensor=None)
    if header.command == "ok":
        depth = 1 / convert_multichannel_depth(depth_tensor)
        depth = cv2.normalize(
            depth, None, alpha=0, beta=255, norm_type=cv2.NORM_MINMAX, dtype=cv2.CV_8UC1
        )
        depth = cv2.applyColorMap(depth, cv2.COLORMAP_MAGMA)
    else:
        depth = np.zeros((480, 640, 3), dtype=np.uint8)

    # Request for rgb
    header, rgb_tensor = client.send_message("get:rgb", tensor=None)
    if header.command == "ok":
        pass
    else:
        rgb_tensor = np.zeros((480, 640, 3), dtype=np.uint8)

    cv2.imshow("depth", depth)
    cv2.imshow("image", cv2.cvtColor(rgb_tensor, cv2.COLOR_RGB2BGR))
    cv2.waitKey(1)
