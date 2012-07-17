//
//  GraphView.m
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "GraphLayer.h"
#import <QuartzCore/QuartzCore.h>
#import "UIColor+Hex.h"
#import "RainData.h"
#import "Coby.h"

@implementation GraphLayer {
    NSArray* _points;
    CAShapeLayer* _borderLayer;
    CAGradientLayer* _gradientLayer;
}

- (id)init {
    if ((self = ([super init]))) {
        self.opaque = YES;
        self.backgroundColor = [UIColor clearColor].CGColor;
        
        _borderLayer = [CAShapeLayer new];
        _borderLayer.strokeColor = [UIColor colorWithHex:0x40a1d9].CGColor;
        _borderLayer.lineWidth = 2;
        _borderLayer.backgroundColor = [UIColor clearColor].CGColor;
        _borderLayer.opaque = NO;
        [self addSublayer:_borderLayer];

        _gradientLayer = [CAGradientLayer new];
        _gradientLayer.colors = [NSArray arrayWithObjects:(id)[UIColor colorWithHex:0x9980ccff].CGColor, [UIColor colorWithHex:0x991e4c67].CGColor, nil];
        [self addSublayer:_gradientLayer];
        
    }
    return self;
}

- (void)layoutSublayers {
    [super layoutSublayers];
    [self generateMaskAnimated:NO];
}

- (void)setRain:(RainData*)rain animated:(BOOL)animated {
    _points = [rain.points take:7];
    [self generateMaskAnimated:animated];
}

- (void)generateMaskAnimated:(BOOL)animated {
    CGFloat width = self.bounds.size.width;
    CGFloat height = self.bounds.size.height;
    CGFloat bottom = self.superlayer.frame.size.height > self.superlayer.frame.size.width ? 40 : 30;
    
    UIBezierPath* path = [UIBezierPath bezierPath];
    [path moveToPoint:(CGPoint) { 0, height }];
    //[path addLineToPoint:(CGPoint) { 0, height-bottom } ];

    CGFloat x = 0;
    CGFloat minY = height-bottom;
    for (RainPoint* point in _points) {
        CGFloat y = ((100 - MAX(point.adjustedIntensity, 5)) / 100.0) * (height-bottom);
        minY = MIN(minY, y);
        [path addLineToPoint:(CGPoint) { x, y }];
        x += width/(_points.count-2);
    }

    //[path addLineToPoint:(CGPoint) { width, height-bottom } ];
    [path addLineToPoint:(CGPoint) { width, height } ];
    [path closePath];
    
    CAShapeLayer* mask = [[CAShapeLayer alloc] init];
    mask.frame = self.bounds;
    mask.path = path.CGPath;
    _gradientLayer.frame = self.bounds;
    _gradientLayer.mask = mask;
    _gradientLayer.startPoint = (CGPoint) { 0, minY/(height-bottom) };
    _gradientLayer.endPoint = (CGPoint) { 0, 1 };
    
    _borderLayer.frame = self.bounds;
    _borderLayer.path = path.CGPath;
}

@end
