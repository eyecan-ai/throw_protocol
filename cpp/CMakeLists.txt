cmake_minimum_required(VERSION 2.8)
project( PostcardCppClient )

add_definitions(-std=c++17)

# find_package( OpenCV REQUIRED )

# add_executable( client_example client_example.cpp )
# target_link_libraries( client_example ${OpenCV_LIBS} )

add_executable( throw_client throw_client.cpp )
# target_link_libraries( client_exthrow_clientample ${OpenCV_LIBS} )