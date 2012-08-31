//
//  RainData.h
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface RainPoint : NSObject

@property (nonatomic, assign, readonly) NSInteger value;
@property (nonatomic, assign, readonly) NSInteger intensity;
@property (nonatomic, assign, readonly) NSInteger adjustedValue;
@property (nonatomic, assign, readonly) CGFloat precipitation;

@end

@interface RainData : NSObject

@property (nonatomic, assign, readonly) NSInteger chance;
@property (nonatomic, strong, readonly) NSArray* points;

- (CGFloat)precipitationForEntries:(int)entries;
- (int)intensityForEntries:(int)entries;
+ (RainData*)rainDataFromString:(NSString*)data;

@end
