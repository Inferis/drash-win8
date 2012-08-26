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
#if TARGET_OS_IPHONE
        self.contentsScale = [[UIScreen mainScreen] scale];
#else
        self.contentsScale = [[NSScreen mainScreen] backingScaleFactor];
#endif
    }
    return self;
}

- (void)drawLayer:(CALayer *)layer inContext:(CGContextRef)context {
    CGFloat mheight = self.bounds.size.height;
    CGFloat bottom = self.bounds.size.height > self.bounds.size.width ? 40 : 30;
    CGFloat height = (mheight - bottom) / 3;
    CGFloat sign = 1;
    
    // white color
#if TARGET_OS_IPHONE
    UIColor* color = [UIColor colorWithWhite:0.85 alpha:1];
    CGContextSetStrokeColorWithColor(context, color.CGColor);
    mheight = 0;
    sign = -1;
#else
    NSColor* color = [NSColor colorWithCalibratedWhite:0.85 alpha:1];
    CGContextSetStrokeColorWithColor(context, color.CGColor);
    CGFloat sign = 1;
#endif
    // dotted
    CGFloat dash[2] = { 1, 3 };
    CGContextSetLineDash(context, 0, dash, 2);
    // 1px
    CGContextSetLineWidth(context, 1.0);
    
    CGContextMoveToPoint(context, 0, mheight - height * sign);
    CGContextAddLineToPoint(context, self.bounds.size.width, mheight - height * sign);
    CGContextMoveToPoint(context, 0, mheight - height*2 * sign);
    CGContextAddLineToPoint(context, self.bounds.size.width, mheight - height*2 * sign);
    CGContextMoveToPoint(context, 30, mheight - height*3 * sign);
    CGContextAddLineToPoint(context, self.bounds.size.width-30, mheight - height*3 * sign);

    CGContextStrokePath(context);

#if TARGET_OS_IPHONE
    UIGraphicsPushContext(context);
    [color setFill];
    UIFont* font = [UIFont fontWithName:@"HelveticaNeue-Light" size:11];
    CGSize sz = [@"0:30" sizeWithFont:font];
    [@"0:00" drawAtPoint:(CGPoint) { 2, height*3 - sz.height/2 } withFont:font];
    [@"0:30" drawAtPoint:(CGPoint) { self.bounds.size.width - 2 - sz.width, height*3 - sz.height/2 } withFont:font];
    UIGraphicsPopContext();
#else
    NSDictionary* attributes = @{
        NSFontAttributeName: [NSFont fontWithName:@"HelveticaNeue-Light" size:11],
        NSForegroundColorAttributeName:color
    };
    CGSize sz = [@"0:30" sizeWithAttributes:attributes];
    [@"0:00" drawAtPoint:(CGPoint) { 2, mheight - 15 - height*3 + sz.height/2 } withAttributes:attributes];
    [@"0:30" drawAtPoint:(CGPoint) { self.bounds.size.width - 2 - sz.width, mheight - 15 - height*3 + sz.height/2 } withAttributes:attributes];
#endif
    
    
}

@end
