//
//  PrecipitationView.h
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import <UIKit/UIKit.h>

@class RainData;

@interface PrecipitationView : UIView

- (void)setRain:(RainData*)rain animated:(BOOL)animated;

@end
