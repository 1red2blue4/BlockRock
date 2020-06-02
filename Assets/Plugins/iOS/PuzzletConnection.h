#import "BLE.h"

@interface PuzzletConnection : NSObject <BLEDelegate>

@property (strong, nonatomic) BLE *ble;

@end
