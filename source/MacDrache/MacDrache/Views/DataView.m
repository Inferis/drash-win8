//
//  DataView.m
//  MacDrache
//
//  Created by Tom Adriaenssen on 05/08/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "DataView.h"
#import "DotsLayer.h"

@implementation DataView {
    DotsLayer* _dotsLayer;
}

- (id)initWithFrame:(NSRect)frame
{
    self = [super initWithFrame:frame];
    if (self) {
        [self setupViews];
    }
    
    return self;
}

- (void)awakeFromNib {
    [super awakeFromNib];
    [self setupViews];
}

- (void)setupViews {
    if (_dotsLayer) return;
    
    self.wantsLayer = YES;
    //self.layer.sublayerTransform = CATransform3DMakeScale(1.0f, -1.0f, 1.0f);
    
    _dotsLayer = [DotsLayer new];
    _dotsLayer.frame = self.layer.bounds;
    [_dotsLayer addConstraint:[CAConstraint constraintWithAttribute:kCAConstraintWidth relativeTo:@"superlayer" attribute:kCAConstraintWidth]];
    [_dotsLayer addConstraint:[CAConstraint constraintWithAttribute:kCAConstraintHeight relativeTo:@"superlayer" attribute:kCAConstraintHeight]];
    [self.layer addSublayer:_dotsLayer];
    [_dotsLayer setNeedsDisplay];
}

- (void)layoutSubtreeIfNeeded {
    [super layoutSubtreeIfNeeded];
    
//    if (_setting)
//        return;
    
    CGFloat bottom = self.bounds.size.height > self.bounds.size.width ? 40 : 30;
    CGFloat height = (self.bounds.size.height - bottom) / 3;
    CGFloat width = self.bounds.size.width;
    
//    _locationLabel.frame = (CGRect) { 0, 0, width, height };
//    _dataView.frame = (CGRect) { (width-320)/2, height, 320, height };
//    _chanceLabel.frame = (CGRect) { 10, 0, floorf(320/1.7777777), height };
//    CGFloat x = CGRectGetMaxX(_chanceLabel.frame);
//    _mmView.frame = (CGRect) { x, 0, 320-x, height };
//    
//    _graphLayer.frame = (CGRect) { -1, height*2, width+2, height + bottom+1 };
    
    _dotsLayer.frame = self.bounds;
    [_dotsLayer setNeedsDisplay];
}

@end
