import throw

socket = throw.ThrowServer.create_accepting_socket("0.0.0.0", 8000)

while True:
    print("Waiting for connection....")
    server = throw.ThrowServer.new_server(
        socket,
        data_callback=lambda header, data: (
            "ok",
            data + 0.1 if data is not None else None,
        ),
    )
