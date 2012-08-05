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
    CGFloat height = (self.bounds.size.height - bottom) / 3;
    
    // white color
#if TARGET_OS_IPHONE
    UIColor* color = [UIColor colorWithWhite:0.85 alpha:1];
    CGContextSetStrokeColorWithColor(context, color.CGColor);
#else
    NSColor* color = [NSColor colorWithCalibratedWhite:0.85 alpha:1];
    CGContextSetStrokeColorWithColor(context, color.CGColor);
#endif
    // dotted
    CGFloat dash[2] = { 1, 3 };
    CGContextSetLineDash(context, 0, dash, 2);
    // 1px
    CGContextSetLineWidth(context, 1.0);
    
    CGContextMoveToPoint(context, 0, mheight - height);
    CGContextAddLineToPoint(context, self.bounds.size.width, mheight - height);
    CGContextMoveToPoint(context, 0, mheight - height*2);
    CGContextAddLineToPoint(context, self.bounds.size.width, mheight - height*2);
    CGContextMoveToPoint(context, 30, mheight - height*3);
    CGContextAddLineToPoint(context, self.bounds.size.width-30, mheight - height*3);

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
//    NSGraphicsContext* ctx = [NSGraphicsContext graphicsContextWithGraphicsPort:context flipped:YES];
//    NSGraphicsContext* wasCtx = [NSGraphicsContext currentContext];
//    [NSGraphicsContext setCurrentContext:ctx];
    
    NSDictionary* attributes = @{
        NSFontAttributeName: [NSFont fontWithName:@"HelveticaNeue-Light" size:11],
        NSForegroundColorAttributeName:color
    };
    CGSize sz = [@"0:30" sizeWithAttributes:attributes];
    [@"0:00" drawAtPoint:(CGPoint) { 2, mheight - 15 - height*3 + sz.height/2 } withAttributes:attributes];
    [@"0:30" drawAtPoint:(CGPoint) { self.bounds.size.width - 2 - sz.width, mheight - 15 - height*3 + sz.height/2 } withAttributes:attributes];

//    [NSGraphicsContext setCurrentContext:wasCtx];
#endif
    
    
}

@end
