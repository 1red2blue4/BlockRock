//
//  extern.m
//

#import "PuzzletConnection.h"
#import "BLE.h"
#import "stdio.h"
#import "string.h"

static PuzzletConnection *puzzletConnection;

@interface PuzzletConnection ()

@end

@implementation PuzzletConnection

@synthesize ble;

//is there an active connection to the device
bool isConnected = false;
bool isEstablishingConnection = false;
//should the device be searching for a device
bool shouldSearchForDevice = true;
//is the board actively searching for a device to connect to
//bool isSearchingForDevice = false;
//how many times have we tried to toggle the notification settings
int notificationToggleCount = 0;
bool dataReceived = false;

//minimum signal strength needed to connect
const double rssiThresh = -60;

char receiver[100];

- (void)initialize
{
    ble = [[BLE alloc] init];
    [ble controlSetup:1];
    ble.delegate = self;

    // call scanning for BTLE peripherals function, need delay for other libraries to load
    [NSTimer scheduledTimerWithTimeInterval:(float)2.0 target:self selector:@selector(turnOnScanConnectionTimer:) userInfo:nil repeats:NO];
}

//used when you need to delay searching for a new connection
-(void) turnOnScanConnectionTimer:(NSTimer *)timer
{
    [self scanForPeripherals];
}

//scans for a device to connect to
-(void) scanForPeripherals
{
    NSLog(@"Try scan for peripherals");
    
    if(/*!isSearchingForDevice &&*/ shouldSearchForDevice){
        NSLog(@"Scan for peripherals");
        //set searching to true
        //isSearchingForDevice = true;
        
        //if there is an active connection, do nothing
        if (ble.activePeripheral && ble.activePeripheral.state == CBPeripheralStateConnected){
            return;
        }
        //if there is no active connection, connect
        if (ble.peripherals)
            ble.peripherals = nil;
        
        [ble findBLEPeripherals:2];
        
        [NSTimer scheduledTimerWithTimeInterval:(float)2.0 target:self selector:@selector(connectionTimer:) userInfo:nil repeats:NO];
    }
}

-(void) connectionTimer:(NSTimer *)timer
{
    bool foundSuitableDevice = false;
    int closestPeripheralIndex = 0;
    
    if (ble.peripherals.count > 0)
    {
        //isSearchingForDevice = false;
        isEstablishingConnection = true;
        //find the index with the lowest RSSI
        for(int ii = 1; ii < ble.peripheralRSSIs.count; ii++){
            if([ble.peripheralRSSIs objectAtIndex:ii]){
                NSLog(@"RSSI strength = %f", [(NSNumber *)[ble.peripheralRSSIs objectAtIndex:ii] doubleValue]);
                if([ble.peripheralRSSIs objectAtIndex:closestPeripheralIndex] == nil || [(NSNumber *)[ble.peripheralRSSIs objectAtIndex:closestPeripheralIndex] doubleValue] < [(NSNumber *)[ble.peripheralRSSIs objectAtIndex:ii] doubleValue]){
                    closestPeripheralIndex = ii;
                }
            }
        }
        
        //threshold check
        if([ble.peripheralRSSIs objectAtIndex:closestPeripheralIndex] != nil)
        {
            if([(NSNumber *)[ble.peripheralRSSIs objectAtIndex:closestPeripheralIndex] doubleValue] > rssiThresh)
                foundSuitableDevice = true;
            else
                UnitySendMessage(receiver, ble.peripherals.count==1? "PuzzletSingleTooFarIOS":"PuzzletSeveralTooFarIOS", "");
        }
    }
    
    if(foundSuitableDevice)
        [ble connectPeripheral:[ble.peripherals objectAtIndex:closestPeripheralIndex]];
    else
    {
        if(!isConnected){
            [self scanForPeripherals];
        }
    }
}

#pragma mark - BLE delegate

- (void)bleDidDisconnect
{
    NSLog(@"->Disconnected");
    
    isConnected = false;
    isEstablishingConnection = false;
    
    if(strlen(receiver) > 0){
        UnitySendMessage(receiver, "PuzzletDisconnectedIOS", "");
    }
    
    //if disconnected, automatically try reconnecting to something
    if(shouldSearchForDevice && !isConnected /*&& !isSearchingForDevice*/){
        [NSTimer scheduledTimerWithTimeInterval:(float)0.5 target:self selector:@selector(turnOnScanConnectionTimer:) userInfo:nil repeats:NO];
    }
}

// When connected, this will be called
-(void) bleDidConnect
{
    isConnected = true;
    isEstablishingConnection = false;
    
    NSLog(@"->Connected");
    
    notificationToggleCount = 0;
    dataReceived = false;
    
    if(!shouldSearchForDevice){
        _disconnect();
    } else {
        if(strlen(receiver) > 0){
            UnitySendMessage(receiver, "PuzzletConnectedIOS", "");
        }
    }
}

-(void) sendData:(Byte *)data length:(int)length
{
    if(!dataReceived){
        notificationToggleCount++;
        if(notificationToggleCount > 10){
            [self externalDisconnect];
        } else {
            NSLog(@"->Toggle notify");
            [ble toggle_notify];
        }
    }
    NSData *buf = [[NSData alloc] initWithBytes:data length:length];
    [ble write:buf];
}

// called every time data is received
-(void) bleDidReceiveData:(unsigned char *)data length:(int)length
{
    dataReceived = true;
    //if a receiver has been set, send it the data contained in the Master Array
    if(strlen(receiver) > 0){
        //generate the update string
        char ret[150] = "";
        char temp[10];
        sprintf(ret, "%d", data[0]);
        for(int ii = 1; ii < length; ii++){
            sprintf(temp, ".%d", data[ii]);
            strcat(ret, temp);
        }
        UnitySendMessage(receiver, "PuzzletReceiveMessageIOS", ret);
    }
}

-(void) externalDisconnect
{
    if(ble.activePeripheral && ble.activePeripheral.state == CBPeripheralStateConnected){
        [[ble CM] cancelPeripheralConnection:[ble activePeripheral]];
    }
}

//*****************
//  EXTERNAL FUNCTIONS
//*****************

//called once when the game starts to initialize the bluetooth
void _setupConnection(void){
    NSAutoreleasePool*		pool = [NSAutoreleasePool new];
    
    puzzletConnection = [PuzzletConnection new];
    [puzzletConnection initialize];
    
    [pool release];
}

void _setReceiver(char* ss){
    strcpy(receiver, ss);
}

//called when the program suspends
void _disconnect(){
    NSLog(@"Disconnect from Unity");
    
    isConnected = false;
    //isSearchingForDevice = false;
    isEstablishingConnection = false;
    
    [puzzletConnection.ble forceDisconnect];
    
    //disconnect from the device, and make sure the connection doesn't immediately get remade
    shouldSearchForDevice = false;
    [puzzletConnection externalDisconnect];
}

//called when the program resumes
void _connect(){
    //try to reestablish a connection to the device (if it's not already)
    shouldSearchForDevice = true;
    NSLog(@"Try connect from Unity");
    NSLog(@" %s", isConnected? "true" : "false");
    //NSLog(@" %s", isSearchingForDevice? "true" : "false");
    NSLog(@" %s", isEstablishingConnection? "true" : "false");
    
    if(!isConnected /*&& !isSearchingForDevice*/ && !isEstablishingConnection){
        NSLog(@"Connect from Unity");
        [puzzletConnection scanForPeripherals];
    }
}

void _sendPacket(Byte* data, int length){
    if(isConnected){
        [puzzletConnection sendData:data length:length];
    }
    free(data);
}

void _checkBluetoothStatus() {
    //init a CB manager to get the system popup if bluetooth is off
    //TODO: check the status of the bluetooth manager
}

@end
