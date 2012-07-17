//
//  DataView.h
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import <UIKit/UIKit.h>

@class RainData;

@interface DataView : UIView

- (void)setRain:(RainData*)rain animated:(BOOL)animated;
- (void)setLocation:(NSString*)location animated:(BOOL)animated;

@end
