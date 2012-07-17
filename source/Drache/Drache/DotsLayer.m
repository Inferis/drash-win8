//
//  DotsLayer.m
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "DotsLayer.h"

@implementation DotsLayer

- (id)init {
    if ((self = [super init])) {
        self.delegate = self;
        self.contentsScale = [[UIScreen mainScreen] scale];
    }
    return self;
}

- (void)drawLayer:(CALayer *)layer inContext:(CGContextRef)context {
    CGFloat bottom = self.bounds.size.height > self.bounds.size.width ? 40 : 30;
    CGFloat height = (self.bounds.size.height - bottom) / 3;
    
    // white color
    UIColor* color = [UIColor colorWithWhite:0.85 alpha:1];
    CGContextSetStrokeColorWithColor(context, color.CGColor);
    // dotted
    CGFloat dash[2] = { 1, 3 };
    CGContextSetLineDash(context, 0, dash, 2);
    // 1px
    CGContextSetLineWidth(context, 1.0);
    
    CGContextMoveToPoint(context, 0, height);
    CGContextAddLineToPoint(context, self.bounds.size.width, height);
    CGContextMoveToPoint(context, 0, height*2);
    CGContextAddLineToPoint(context, self.bounds.size.width, height*2);
    CGContextMoveToPoint(context, 30, height*3);
    CGContextAddLineToPoint(context, self.bounds.size.width-30, height*3);

    // and now draw the Path!
    CGContextStrokePath(context);

    UIGraphicsPushContext(context);
    UIFont* font = [UIFont fontWithName:@"HelveticaNeue-Light" size:11];
    CGSize sz = [@"0:30" sizeWithFont:font];
    
    [color setFill];
    [@"0:00" drawAtPoint:(CGPoint) { 2, height*3 - sz.height/2 } withFont:font];
    [@"0:30" drawAtPoint:(CGPoint) { self.bounds.size.width - 2 - sz.width, height*3 - sz.height/2 } withFont:font];
    UIGraphicsPopContext();
    
}

@end
