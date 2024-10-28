// CppClient.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <Windows.h>
#include <TcAdsDef.h>
#include <TcAdsAPI.h>

int main() {
    // Create a long variable to store the ADS error status
    long nErr, nPort;

    // Create an AmsAddr structure to address the ADS device
    AmsAddr addr;

    // Open the connection to the local ADS router
    nPort = AdsPortOpen();
    std::cout << "nPort: " << nPort << std::endl;
    nErr = AdsGetLocalAddress(&addr);
    if (nErr) {
        std::cerr << "Error: AdsGetLocalAddress failed with error " << nErr << std::endl;
        return 1;
    }
    std::cout << "addr: " << static_cast<unsigned>(addr.netId.b[0]) << std::endl;
    std::cout << "addr: " << static_cast<unsigned>(addr.netId.b[1]) << std::endl;
    std::cout << "addr: " << static_cast<unsigned>(addr.netId.b[2]) << std::endl;
    std::cout << "addr: " << static_cast<unsigned>(addr.netId.b[3]) << std::endl;
    std::cout << "addr: " << static_cast<unsigned>(addr.netId.b[4]) << std::endl;
    std::cout << "addr: " << static_cast<unsigned>(addr.netId.b[5]) << std::endl;

    // Set the NetId and port of the target device
    // Example: 192.168.1.1.1.1 is the AMS Net ID and 851 is the AMS port of the target
/*
    addr.netId.b[0] = 192;
    addr.netId.b[1] = 168;
    addr.netId.b[2] = 0;
    addr.netId.b[3] = 105;
    addr.netId.b[4] = 1;
    addr.netId.b[5] = 1;
*/
    addr.port = 851;

    // Read device info
    /*
    AdsVersion version;
    char * devName = new char[16];
    nErr = AdsSyncReadDeviceInfoReq(&addr, devName, &version);
    if (nErr) {
        std::cerr << "Error: AdsSyncReadDeviceInfoReq failed with error " << nErr << std::endl;
        return 1;
    }
    */
    USHORT nAdsState;
    USHORT    nDeviceState;
    nErr = AdsSyncReadStateReq(&addr, &nAdsState, &nDeviceState);
    if (nErr) {
        std::cerr << "Error: AdsSyncReadStateReq failed with error " << nErr << std::endl;
        return 1;
    }

    // Output the device information
    /*
    std::cout << "Device Name: " << devName << std::endl;
    std::cout << "Version: " << static_cast<int>(version.version) << '.'
        << static_cast<int>(version.revision) << std::endl;
    std::cout << "Build: " << version.build << std::endl;
    */
    std::cout << "Ads state: " << nAdsState << std::endl;
    std::cout << "Ads device state: " << nDeviceState << std::endl;

    // Close ADS connection
    AdsPortClose();

    return 0;
}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
