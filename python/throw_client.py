import numpy as np
import throw
import sys

client = throw.ThrowClient(sys.argv[1], int(sys.argv[2]))
tensor = np.eye(4).reshape((4, 4, 1)).astype(np.float32)

while True:

    header, tensor = client.send_message("sample_command", tensor=tensor)

    print(tensor.reshape(4, 4))
