import sys

sys.path.append("../../../python")
import numpy as np
import throw
import sys
import time
import numpy as np
import transforms3d

client = throw.ThrowClient(sys.argv[1], int(sys.argv[2]))
tensor = np.eye(4).reshape((4, 4, 1)).astype(np.float32)

# parameters
start_time = time.time()
magnitude = 0.4
frequency = 0.1
start_transform = np.array([[1, 0, 0, 0], [0, 0, 1, 0], [0, -1, 0, 0], [0, 0, 0, 1]])

while True:

    # compute sinusoidal motion
    elapsed_time = time.time() - start_time
    sinpoint = np.sin(elapsed_time * np.pi * frequency)
    cospoint = np.cos(elapsed_time * np.pi * frequency)

    # Build Translation (a circle)
    DT = np.eye(4)
    DT[0, 3] = magnitude * cospoint
    DT[1, 3] = magnitude * sinpoint
    T = np.dot(start_transform, DT)

    # Build Rotation (ration coherent with circle)
    RT = np.eye(4)
    angle = np.arctan2(sinpoint, cospoint)
    RT[:3, :3] = transforms3d.euler.euler2mat(0, 0, angle, "rxyz")
    T = np.dot(T, RT)

    # Convert transform to tensor H,W,D [float32]
    T = T.reshape((4, 4, 1)).astype(np.float32)

    # Send data
    header, tensor = client.send_message("sample_command", tensor=T)

    # hz control
    time.sleep(0.001)
