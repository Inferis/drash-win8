//
//  DataView.h
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import <UIKit/UIKit.h>

@interface DataView : UIView

- (void)setPercentage:(int)percentage precipitation:(float)precipitation intensity:(int)intensity animated:(BOOL)animated;

- (void)setInvalidPercentageAnimated:(BOOL)animated;

- (void)setLocation:(NSString*)location animated:(BOOL)animated;

@end
