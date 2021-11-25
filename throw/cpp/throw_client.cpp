#include <algorithm>
#include <arpa/inet.h> /* for sockaddr_in and inet_addr() */
#include <cstdint>
#include <cstring>
#include <iostream>
#include <netinet/tcp.h>
#include <stdio.h>  /* for printf() and fprintf() */
#include <stdlib.h> /* for atoi() and exit() */
#include <string.h> /* for memset() */
#include <string>
#include <sys/socket.h> /* for socket(), connect(), send(), and recv() */
#include <sys/time.h>
#include <unistd.h> /* for close() */

#include "Throw.hpp"

void die(std::string errorMessage)
{
  std::cout << errorMessage << std::endl;
  exit(1);
}

int main(int argc, char *argv[])
{

  /* Socket parameters */
  int sock;                         /* Socket descriptor */
  struct sockaddr_in echoServAddr;  /* Echo server address */
  unsigned short serverPort = 8000; /* Echo server port */

  if (argc < 3)
  {
    std::cout << "Not enought arguments..." << std::endl;
    exit(1);
  }

  int times = 100000000;
  if (argc>3)
  {
    times = atoi(argv[3]);
  }

  throwprotocol::ThrowClientExample client(argv[1], atoi(argv[2]));

  
  float* data = new float[16]{
      1.0, 0.0, 0.0, 0.0,
      0.0, 1.0, 0.0, 0.0,
      0.0, 0.0, 1.0, 0.0,
      0.0, 0.0, 0.0, 1.0
  };


  for (; times > 0; times--)
  {
    
    int height = 4;
    int width = 4;
    int depth = 1;
    int bytes_per_element = 4;
    std::shared_ptr<float> received_data = client.sendAndReceiveData(data, height, width, depth, bytes_per_element, "eye_matrix");

    for (int i = 0; i < height; i++)
    {
      for (int j = 0; j < width; j++)
      {
        data[i * width + j] = received_data.get()[i * width + j];
        std::cout << received_data.get()[i * width + j] << " ";
      }
      std::cout << std::endl;
    }

  }
  
  
}