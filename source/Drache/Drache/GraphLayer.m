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
#import "NSUserDefaults+Settings.h"

@implementation GraphLayer {
    NSArray* _points;
    CAShapeLayer* _borderLayer;
    CAGradientLayer* _gradientLayer;
    CAShapeLayer* _mask;
    int _entries;
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
        
        _mask = [[CAShapeLayer alloc] init];
        _gradientLayer.mask = _mask;
        _entries = 6;
        
        [self generateMaskAnimated:NO];
    }
    return self;
}

- (void)layoutSublayers {
    [super layoutSublayers];
    [self generateMaskAnimated:NO];
}

- (void)setRain:(RainData*)rain animated:(BOOL)animated {
    _points = rain.points;
    [self generateMaskAnimated:animated];
}

- (void)generateMaskAnimated:(BOOL)animated {
    CGFloat width = self.bounds.size.width;
    CGFloat height = self.bounds.size.height;
    CGFloat bottom = self.superlayer.frame.size.height > self.superlayer.frame.size.width && !IsIPad() ? 40 : 30;
    
    UIBezierPath* path = [UIBezierPath bezierPath];
    [path moveToPoint:(CGPoint) { 0, height }];

    CGFloat max = (height-bottom);
    CGFloat minY = max;
    CGFloat x = 0;
    BOOL allZero = YES;
    
    int entries = [[NSUserDefaults standardUserDefaults] entries];
    entries = MIN(MAX(entries, 6), _points.count);
    
    NSArray* points = _points;
    if (points) {
        allZero = ![points any:^BOOL(RainPoint* point) {
            return point.adjustedValue > 0;
        }];

        if (!allZero) {
            for (RainPoint* point in points) {
                CGFloat y = ((100 - MAX(point.adjustedValue, 7)) / 100.0) * (height-bottom);
                minY = MIN(minY, y);
                [path addLineToPoint:(CGPoint) { x, y }];
                x += width/entries;
            }
            [path addLineToPoint:(CGPoint) { x, (93.0 / 100.0) * (height-bottom) }];
        }
    }
    
    NSArray* newColors;
    if (allZero) {
        for (int i=0; i<points.count; ++i) {
            [path addLineToPoint:(CGPoint) { x, 0 }];
            x += width/entries;
        }
        minY = (height-bottom)/2.0;
        newColors = [NSArray arrayWithObjects:(id)[UIColor clearColor].CGColor, [UIColor colorWithHex:0x991e4c67].CGColor, nil];
    }
    else
        newColors = [NSArray arrayWithObjects:(id)[UIColor colorWithHex:0x9980ccff].CGColor, [UIColor colorWithHex:0x991e4c67].CGColor, nil];

    [path addLineToPoint:(CGPoint) { width, height } ];
    [path closePath];
    
    animated = entries != _entries && _entries > 0 && entries > 0;
    _entries = entries;
    
    _gradientLayer.frame = self.bounds;
    _borderLayer.frame = self.bounds;
    _mask.frame = self.bounds;

    _borderLayer.opacity = allZero ? 0 : 1;
    if (animated) {
        CABasicAnimation *animation = [CABasicAnimation animationWithKeyPath:@"path"];
        animation.duration = 0.3;
        animation.timingFunction = [CAMediaTimingFunction functionWithName:kCAMediaTimingFunctionEaseInEaseOut];
        animation.fromValue = (id)_mask.path;
        animation.toValue = (id)path.CGPath;
        [_mask addAnimation:animation forKey:@"animatePath"];
    }
    _mask.path = path.CGPath;

    CGPoint newStartPoint = (CGPoint) { 0, minY/(height) };
    if (animated) {
        CABasicAnimation *animation = [CABasicAnimation animationWithKeyPath:@"startPoint"];
        animation.duration = 0.3;
        animation.timingFunction = [CAMediaTimingFunction functionWithName:kCAMediaTimingFunctionEaseInEaseOut];
        animation.fromValue = [NSValue valueWithCGPoint:_gradientLayer.startPoint];
        animation.toValue = [NSValue valueWithCGPoint:newStartPoint];
        [_gradientLayer addAnimation:animation forKey:@"animateStartPoint"];

        animation = [CABasicAnimation animationWithKeyPath:@"colors"];
        animation.duration = 0.3;
        animation.timingFunction = [CAMediaTimingFunction functionWithName:kCAMediaTimingFunctionEaseInEaseOut];
        animation.fromValue = _gradientLayer.colors;
        animation.toValue = newColors;
        [_gradientLayer addAnimation:animation forKey:@"animateColors"];
    }
    _gradientLayer.startPoint = newStartPoint;
    _gradientLayer.endPoint = (CGPoint) { 0, 1 };
    _gradientLayer.colors = newColors;
    
    if (animated) {
        CABasicAnimation *animation = [CABasicAnimation animationWithKeyPath:@"path"];
        animation.duration = 0.3;
        animation.timingFunction = [CAMediaTimingFunction functionWithName:kCAMediaTimingFunctionEaseInEaseOut];
        animation.fromValue = (id)_borderLayer.path;
        animation.toValue = (id)path.CGPath;
        [_borderLayer addAnimation:animation forKey:@"animatePath"];
    }
    _borderLayer.path = path.CGPath;
}

@end
