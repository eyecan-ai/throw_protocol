import sys

sys.path.append("../../../python")
import numpy as np
import throw
import sys
import time
import numpy as np
import cv2

client = throw.ThrowClient(sys.argv[1], int(sys.argv[2]))

while True:

    # Send data
    header, tensor = client.send_message("sample_command", tensor=None)

    print(tensor.min(), tensor.max(), tensor.dtype)

    cv2.imshow("image", cv2.cvtColor(tensor.astype(np.uint8), cv2.COLOR_RGB2BGR))
    cv2.waitKey(1)
