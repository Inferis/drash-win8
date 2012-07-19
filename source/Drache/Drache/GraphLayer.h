//
//  GraphView.h
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import <UIKit/UIKit.h>
#import <QuartzCore/QuartzCore.h>

@class  RainData;

@interface GraphLayer : CALayer

- (void)setRain:(RainData*)rain animated:(BOOL)animated;

@end
