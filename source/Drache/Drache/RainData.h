//
//  RainData.h
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface RainData : NSObject

@property (nonatomic, assign, readonly) NSInteger intensity;
@property (nonatomic, assign, readonly) CGFloat precipitation;
@property (nonatomic, assign, readonly) NSInteger chance;
@property (nonatomic, strong, readonly) NSArray* points;

+ (RainData*)rainDataFromString:(NSString*)data;

@end
