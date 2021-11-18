# Throw simple protocol


## 1) Start python server

From root folder

```
cd python
python throw_server.py
```

## 2.2) Start python client

From root folder

```
cd python
python throw_client.py 127.0.0.1 8000
```

## 2.1) Start cpp client

From root folder

```
cd cpp
mkdir build
cd build
cmake ..
make
./throw_client 127.0.0.1 8000
```

or, on windows:

```
cd cpp
mkdir build
cd build
cmake ..
make
throw_client.exe 127.0.0.1 8000
```

The **CPP Client Example** will connect to a Throw Server running locally (change 127.0.0.01 with remote custom IP address).

It will send a *4x4x1* **float32** matrix to the server which will add a small ammount to each matrix cell (*e.g* ```+0.1```). The server will receive the updated *4x4x1* matrix and send back resulting in a continous matrix increnent.
