import socket
import struct
from typing import Callable, Optional, Tuple
import numpy as np
import threading
from PIL import Image
import io
from loguru import logger


class DataManipulator(object):
    @staticmethod
    def bytes_to_image(buffer: bytes) -> np.ndarray:
        """Convert bytes to image. Used to convert compressed image data to numpy array.

        :param buffer: raw data
        :type buffer: bytes
        :return: image built from raw data
        :rtype: np.ndarray
        """
        return np.array(Image.open(io.BytesIO(buffer)))

    @staticmethod
    def image_to_bytes(image: np.ndarray, format: str = "png") -> bytes:
        """Convert image to bytes. Used to convert numpy array to compressed image data.

        :param image: image to convert
        :type image: np.ndarray
        :param format: image codec (same as extension) , defaults to "png"
        :type format: str, optional
        :return: raw data
        :rtype: bytes
        """
        buffer = io.BytesIO()
        pilimage = Image.fromarray(image)
        pilimage.save(buffer, format=format)
        buffer = buffer.getvalue()
        return buffer

    @staticmethod
    def bytes_to_tensor(
        buffer: bytes, height: int, width: int, depth: int, bytes_per_element: int
    ) -> np.ndarray:
        """Convert bytes to tensor. Used to convert raw image data bytes to numpy array.

        :param buffer: raw data
        :type buffer: bytes
        :param height:  tensor height
        :type height: int
        :param width: tensor width
        :type width: int
        :param depth: tensor depth
        :type depth: int
        :param bytes_per_element: bytes per element
        :type bytes_per_element: int
        :raises ValueError: if bytes_per_element is not recognized as valid
        :return: tensor built from raw data
        :rtype: np.ndarray
        """
        tensor = None
        if bytes_per_element == 1:
            tensor = np.frombuffer(buffer, dtype=np.uint8)
        elif bytes_per_element == 2:
            tensor = np.frombuffer(buffer, dtype=np.uint16)
        elif bytes_per_element == 4:
            tensor = np.frombuffer(buffer, dtype=np.float32)
        elif bytes_per_element == 8:
            tensor = np.frombuffer(buffer, dtype=np.float64)
        else:
            raise ValueError("Invalid bytes per element")

        tensor = tensor.reshape(height, width, depth)
        return tensor

    @staticmethod
    def tensor_to_bytes(
        tensor: np.ndarray,
    ) -> Tuple[bytes, int, int, int, int]:
        """Convert tensor to bytes. Used to convert numpy array to raw image data bytes.

        :param tensor: tensor to convert
        :type tensor: np.ndarray
        :raises ValueError: if tensor data type is not recognized as valid
        :return: (raw data, height, width, depth, bytes per element)
        :rtype: Tuple[bytes, int, int, int, int]
        """

        height, width, depth = tensor.shape

        bytes_map = {"uint8": 1, "uint16": 2, "float32": 4, "float64": 8}

        dtype = str(tensor.dtype)
        if dtype not in bytes_map:
            raise ValueError(f"Invalid dtype: {dtype}")

        bytes_per_element = bytes_map[dtype]
        return (tensor.tobytes(), height, width, depth, bytes_per_element)


class Header(object):

    HEADER_COMMAND_LEN = 32
    HEADER_SIZE = 52
    HEADER_FORMAT = fmt = "BBBBiiii" + ("B" * 32)

    def __init__(self, raw_data: Optional[bytes] = None):
        """Initialize Header from raw data

        :param raw_data: raw header data , defaults to None
        :type raw_data: Optional[bytes], optional
        """

        if raw_data is not None:
            self.received_data = Header.unpacker().unpack(raw_data)
            self.crc = self.received_data[0:4]
            self.width = self.received_data[4]
            self.height = self.received_data[5]
            self.depth = self.received_data[6]
            self.byte_per_element = self.received_data[7]
            self.command = str(bytes(self.received_data[8:]), "utf-8").rstrip("\0")
            self.command = self.command.strip()
        else:
            self.received_data = None
            self.crc = (0, 0, 0, 0)
            self.width = 0
            self.height = 0
            self.depth = 0
            self.byte_per_element = 0
            self.command = ""

    @property
    def payload_size(self) -> int:
        """Get payload size

        :return: expected payload size
        :rtype: int
        """
        return self.width * self.height * self.depth * self.byte_per_element

    def pack(self) -> bytes:
        """Pack Header in raw bytes

        :return: raw bytes
        :rtype: bytes
        """
        command_ext = self.command.ljust(Header.HEADER_COMMAND_LEN)
        command_ext = bytes(command_ext, "utf-8")
        return struct.pack(
            Header.HEADER_FORMAT,
            *self.crc,
            self.width,
            self.height,
            self.depth,
            self.byte_per_element,
            *command_ext,
        )

    @staticmethod
    def unpacker() -> struct.Struct:
        """Build unpacker for Header

        :return: unpacked struct
        :rtype: struct.Struct
        """
        return struct.Struct(Header.HEADER_FORMAT)

    def __repr__(self) -> str:
        return f"Header('{self.command}',{self.height},{self.width},{self.depth},{self.byte_per_element})"


class ThrowServer(object):
    ACTIVE_THREADS = []

    def __init__(
        self,
        connection: socket.socket,
        client_address: Tuple[str, int],
        data_callback: Callable = None,
        auto_start: bool = True,
    ):
        """Initialize ThrowServer

        :param connection: socket connection
        :type connection: socket.socket
        :param client_address: client address (0.0.0.0 for any)
        :type client_address: Tuple[str, int]
        :param data_callback: callback for received data , defaults to None
        :type data_callback: Callable, optional
        :param auto_start: TRUE to auto start waiting server, defaults to True
        :type auto_start: bool, optional
        """
        self.connection = connection
        self.client_address = client_address
        self.data_callback = data_callback
        self.thread = threading.Thread(target=self.run)
        ThrowServer.ACTIVE_THREADS.append(self.thread)
        if auto_start:
            self.thread.start()

    def start(self):
        """Start main thread"""
        self.thread.start()

    @classmethod
    def receive_data_from_connection(
        cls, connection: socket.socket, payload_size: int
    ) -> bytes:
        """Receive data from socket connection

        :param connection: socket connection
        :type connection: socket.socket
        :param payload_size: bytes to receive
        :type payload_size: int
        :return: received bytes
        :rtype: bytes
        """

        logger.debug(f"Waiting for {payload_size} bytes ...")
        received_size = 0
        received_data = b""
        while len(received_data) < payload_size:
            chunk = connection.recv(payload_size - received_size)
            if not chunk:
                break
            received_data += chunk

        logger.debug(f"Payload received: {len(received_data)}")
        return received_data

    @classmethod
    def data_to_tensor(cls, header: Header, data: bytes) -> np.ndarray:
        """Convert raw data to numpy array

        :param header: data header
        :type header: Header
        :param data: raw data bytes
        :type data: bytes
        :return: converted tensor
        :rtype: np.ndarray
        """
        if header.height == 1 and header.depth == 1 and header.depth > 0:
            tensor = DataManipulator.bytes_to_image(data)
        else:
            tensor = DataManipulator.bytes_to_tensor(
                data,
                header.height,
                header.width,
                header.depth,
                header.byte_per_element,
            )
        return tensor

    @classmethod
    def tensor_to_header_and_data(
        self, command: str, tensor: np.ndarray
    ) -> Tuple[Header, bytes]:
        """Convert tensor to (header, data)

        :param command: textual command
        :type command: str
        :param tensor: tensor to convert
        :type tensor: np.ndarray
        :return: created (header, data)
        :rtype: Tuple[Header, bytes]
        """

        header = Header()
        header.command = command
        if tensor is not None:
            (
                data,
                header.height,
                header.width,
                header.depth,
                header.byte_per_element,
            ) = DataManipulator.tensor_to_bytes(tensor)
        else:
            (
                data,
                header.height,
                header.width,
                header.depth,
                header.byte_per_element,
            ) = (None, 0, 0, 0, 0)
        return header, data

    def run(self):
        """Main loop"""

        while True:
            # Receive Header
            logger.debug("Waiting for Header ...")
            header = Header(self.connection.recv(Header.HEADER_SIZE))
            logger.debug(f"Received: {str(header)}")

            tensor = None
            if header.payload_size > 0:

                # Receive Payload
                received_data = self.receive_data_from_connection(
                    self.connection, header.payload_size
                )

                # Payload to Tensor
                tensor = self.data_to_tensor(header, received_data)

            command = ""
            response_image = None
            if self.data_callback is not None:
                command, response_image = self.data_callback(header, tensor)
            self.send_response(command, response_image)

    def send_response(self, command: str, tensor: np.ndarray = None):
        """Send response to client

        :param command: textual command
        :type command: str
        :param tensor: tensor to send if any , defaults to None
        :type tensor: np.ndarray, optional
        """

        response_header, response_data = self.tensor_to_header_and_data(command, tensor)

        # Send Header
        self.connection.send(response_header.pack())
        logger.debug("Response Header sent!")

        # Send data
        if response_data is not None:
            self.connection.send(response_data)
        logger.debug("Response Payload sent!")

    @staticmethod
    def new_server(
        socket: socket.socket,
        data_callback: Optional[Callable] = None,
    ) -> "ThrowServer":
        """Create new server from accepting socket

        :return: created server
        :rtype: ThrowServer
        """
        connection, address = socket.accept()
        return ThrowServer(connection, address, data_callback=data_callback)

    @staticmethod
    def create_accepting_socket(
        host: str = "0.0.0.0", port: int = 8000
    ) -> socket.socket:
        """Create accepting socket

        :param host: remote allowed address, defaults to "0.0.0.0"
        :type host: str, optional
        :param port: port, defaults to 8000
        :type port: int, optional
        :return: accepting socket
        :rtype: socket.socket
        """
        # Create a TCP/IP socket
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        server_address = (host, port)
        sock.bind(server_address)
        sock.listen(1)
        return sock


class ThrowClient(object):
    def __init__(self, host: str, port: int):
        self.host = host
        self.port = port
        self._socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self._socket.connect((host, port))
        self.connection = self._socket
        logger.debug(f"Client Connected to: {host}:{port}")

    def send_message(
        self, command: str, tensor: Optional[np.ndarray] = None
    ) -> Tuple[str, Optional[np.ndarray]]:
        """Send message to server and receive response if any

        :param command: textual command
        :type command: str
        :param tensor: tensor to send , defaults to None
        :type tensor: Optional[np.ndarray], optional
        :return: received command and tensor if any
        :rtype: Tuple[str, Optional[np.ndarray]]
        """

        logger.debug(f"Sending message ({command})")

        sending_header, sending_data = ThrowServer.tensor_to_header_and_data(
            command, tensor
        )

        # Send Header
        self.connection.send(sending_header.pack())

        # Send data
        if sending_data is not None:
            self.connection.send(sending_data)

        # Receive Header
        logger.debug("Waiting for Header ...")
        data = self.connection.recv(Header.HEADER_SIZE)
        header = Header(data)
        logger.debug(f"Received: {header.command}")

        tensor = None
        if header.payload_size > 0:

            # Receive Payload
            received_data = ThrowServer.receive_data_from_connection(
                self.connection, header.payload_size
            )

            # Payload to Tensor
            tensor = ThrowServer.data_to_tensor(header, received_data)

        return header, tensor
