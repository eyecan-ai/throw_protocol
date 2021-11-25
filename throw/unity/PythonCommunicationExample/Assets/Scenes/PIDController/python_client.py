import sys
from typing import Dict, Sequence, Tuple

sys.path.append("../../../python")
import numpy as np
import throw
import sys
import time
import numpy as np
import threading
import cv2
from loguru import logger

logger.remove()


class LivePlotter:
    def __init__(
        self,
        x_range: Tuple[int, int] = [0, 800],
        y_range: Tuple[int, int] = [0, 300],
        x_scale: float = 1.0,
        y_scale: float = 10.0,
        y_offset: float = 150,
        colors: Dict[str, Tuple[int, int, int]] = {},
        thicknesses: Dict[str, int] = {},
        background_color: Tuple[int, int, int] = (100, 100, 100),
    ) -> None:

        self._x_range = x_range
        self._y_range = y_range
        self._x_scale = x_scale
        self._y_scale = y_scale
        self._y_offset = y_offset

        self._background_color = background_color
        self._thicknesses = thicknesses
        self._canvas = self._reset_canvas()
        self._data = {}
        self._colors = colors
        self._internal_counter = 0

    def auto_scale_y(self):
        """Automatically scale the y axis to fit the data"""
        y_all_data = []
        for key, pairs in self._data.items():
            y_all_data.extend([y for x, y in pairs])
        y_all_data = np.array(y_all_data)
        min_y = y_all_data.min()
        max_y = y_all_data.max()
        range = np.abs(max_y - min_y)
        self._y_scale = self.y_size / range
        self._y_offset = min_y

    def reset(self):
        """Reset the data and canvas"""
        self._data = {}
        self._canvas = self._reset_canvas()
        self._internal_counter = 0

    def _reset_canvas(self) -> np.ndarray:
        """Reset the canvas to the background color

        :return: empty canvas
        :rtype: np.ndarray
        """
        return np.full(
            (self.y_size, self.x_size, 3), self._background_color, dtype=np.uint8
        )

    @property
    def canvas(self) -> np.ndarray:
        return self._canvas

    @property
    def x_size(self):
        return self._x_range[1] - self._x_range[0]

    @property
    def y_size(self):
        return self._y_range[1] - self._y_range[0]

    def push(self, y_data: dict, x: int = -1):
        """Push a new data point to the plotter

        :param y_data: single data point for eack key
        :type y_data: dict
        :param x: time (-1 to auto time data), defaults to -1
        :type x: int, optional
        """
        if x == -1:
            x = self._internal_counter
            self._internal_counter += 1

        for key, value in y_data.items():
            if key not in self._data:
                self._data[key] = []
            self._data[key].append((x, value))

    def update_canvas(self):
        """Draw the canvas with the stored data"""
        self._canvas = self._reset_canvas()
        for key, pairs in self._data.items():
            color = self._colors.get(key, (255, 255, 255))
            thikness = self._thicknesses.get(key, 2)

            last_point = None
            for pair_idx, pair in enumerate(pairs):
                point = (
                    np.array([*pairs[pair_idx]])
                    * np.array([self._x_scale, self._y_scale]).copy()
                )
                point[1] = self.y_size - self._y_offset - point[1]
                cv2.circle(
                    self._canvas,
                    tuple(np.int32(point)),
                    thikness // 2,
                    color,
                    -1,
                    lineType=cv2.LINE_AA,
                )
                if last_point is not None:
                    cv2.line(
                        self._canvas,
                        tuple(np.int32(point)),
                        tuple(np.int32(last_point)),
                        color=color,
                        thickness=thikness,
                        lineType=cv2.LINE_AA,
                    )
                last_point = point


# Create Live Plotter for Position/Velocity/Force
plotter = LivePlotter(
    colors={"position": (255, 255, 0), "velocity": (0, 255, 0), "force": (255, 0, 0)}
)

# Throw client
client = throw.ThrowClient(sys.argv[1], int(sys.argv[2]))

# Controller Parameters
start_time = time.time()
magnitude = 0.4
frequency = 0.1
force = 0.0
max_force = 25.0
P = 5.0
D = -50.0
target_position = 0.5

while True:

    # Prepare data to send [Force]
    tensor = np.array([force]).reshape((1, 1, 1)).astype(np.float32)

    # Send data
    header, response_tensor = client.send_message("sample_command", tensor=tensor)

    # Retrieve Data
    position, velocity, _ = response_tensor.ravel()

    # Compute control
    force = P * (1 / (np.abs(position - target_position + 1e-6)))
    force += D * velocity if velocity < 0 else 0

    # Clip force
    force = np.clip(force, 0, max_force)

    # Plot live data
    plotter.push(y_data={"position": position, "force": force, "velocity": velocity})
    plotter.update_canvas()

    # Show live plot
    cv2.imshow("Canvas", cv2.cvtColor(plotter.canvas, cv2.COLOR_RGB2BGR))
    q = cv2.waitKey(10)
    if q == ord("r"):
        plotter.reset()
    if q == ord("s"):
        plotter.auto_scale_y()
