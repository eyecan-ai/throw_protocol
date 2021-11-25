==============
throw-protocol
==============


.. image:: https://img.shields.io/pypi/v/throw_protocol.svg
        :target: https://pypi.python.org/pypi/throw_protocol

.. image:: https://img.shields.io/travis/eyecan-ai/throw_protocol.svg
        :target: https://travis-ci.com/eyecan-ai/throw_protocol

.. image:: https://readthedocs.org/projects/throw-protocol/badge/?version=latest
        :target: https://throw-protocol.readthedocs.io/en/latest/?version=latest
        :alt: Documentation Status




Python Boilerplate contains all the boilerplate you need to create a Python package.


* Free software: MIT license
* Documentation: https://throw-protocol.readthedocs.io.


Features
--------


1) Start python server
======================

From root folder:

.. code-block:: bash

        cd python
        python throw_server.py


2.1) Start python client
========================

From root folder:

.. code-block:: bash

        cd python
        python throw_client.py 127.0.0.1 8000


2.2) Start cpp client
=====================

From root folder:

.. code-block:: bash

        cd cpp
        mkdir build
        cd build
        cmake ..
        make
        ./throw_client 127.0.0.1 8000


or, on windows:

.. code-block:: bash

        cd cpp
        mkdir build
        cd build
        cmake ..
        make
        throw_client.exe 127.0.0.1 8000


The **CPP Client Example** will connect to a Throw Server running locally (change 127.0.0.01 with remote custom IP address).

It will send a *4x4x1* **float32** matrix to the server which will add a small ammount to each matrix cell (*e.g* ``+0.1``). The server will receive the updated *4x4x1* matrix and send back resulting in a continous matrix increnent.


Credits
-------

This package was created with Cookiecutter_ and the `audreyr/cookiecutter-pypackage`_ project template.

.. _Cookiecutter: https://github.com/audreyr/cookiecutter
.. _`audreyr/cookiecutter-pypackage`: https://github.com/audreyr/cookiecutter-pypackage
