//
//  DataView.m
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "DataView.h"
#import "UIView+Pop.h"
#import "RainData.h"
#import "PrecipitationView.h"
#import "GraphLayer.h"
#import <QuartzCore/QuartzCore.h>
#import "DotsLayer.h"

@implementation DataView {
    UILabel* _locationLabel;
    UILabel* _chanceLabel;
    PrecipitationView* _mmView;
    UIView* _dataView;
    BOOL _setting;
    GraphLayer* _graphLayer;
    DotsLayer* _dotsLayer;
}

- (id)initWithFrame:(CGRect)frame
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
    self.backgroundColor = [UIColor clearColor];
    self.opaque = NO;
    
    
    // build location label
    _locationLabel = [[UILabel alloc] init];
    _locationLabel.alpha = 0;
    _locationLabel.opaque = NO;
    _locationLabel.textColor = [UIColor whiteColor];
    _locationLabel.backgroundColor = [UIColor clearColor];
    _locationLabel.textAlignment = UITextAlignmentCenter;
    _locationLabel.font = [UIFont fontWithName:@"HelveticaNeue-Light" size:20];
    [self addSubview:_locationLabel];

    // data container view for animation purposes
    _dataView = [UIView new];
    _dataView.opaque = NO;
    _dataView.backgroundColor = [UIColor clearColor];
    [self addSubview:_dataView];

    _chanceLabel = [[UILabel alloc] init];
    _chanceLabel.opaque = NO;
    _chanceLabel.text = @"?";
    _chanceLabel.textColor = [UIColor whiteColor];
    _chanceLabel.backgroundColor = [UIColor clearColor];
    _chanceLabel.textAlignment = UITextAlignmentCenter;
    _chanceLabel.font = [UIFont fontWithName:@"HelveticaNeue-UltraLight" size:90];
    [_dataView addSubview:_chanceLabel];
    
    _mmView = [PrecipitationView new];
    [_dataView addSubview:_mmView];
    
    _graphLayer = [GraphLayer new];
    [self.layer addSublayer:_graphLayer];
    
    _dotsLayer = [DotsLayer new];
    [self.layer addSublayer:_dotsLayer];
    
    [self setRain:nil animated:NO];
}

- (void)layoutSubviews {
    [super layoutSubviews];

    if (_setting)
        return;
    
    CGFloat bottom = self.bounds.size.height > self.bounds.size.width ? 40 : 30;
    CGFloat height = (self.bounds.size.height - bottom) / 3;
    CGFloat width = self.bounds.size.width;
    
    _locationLabel.frame = (CGRect) { 0, 0, width, height };
    _dataView.frame = (CGRect) { (width-320)/2, height, 320, height };
    _chanceLabel.frame = (CGRect) { 10, 0, floorf(320/1.7777777), height };
    CGFloat x = CGRectGetMaxX(_chanceLabel.frame);
    _mmView.frame = (CGRect) { x, 0, 320-x, height };
    
    _graphLayer.frame = (CGRect) { -1, height*2, width+2, height + bottom+1 };

    _dotsLayer.frame = self.bounds;
    [_dotsLayer setNeedsDisplay];
}

- (void)setRain:(RainData*)rain animated:(BOOL)animated {
    NSString* chanceText;
    UIColor* chanceColor;

    if (rain && rain.chance >= 0) {
        chanceText = [NSString stringWithFormat:@"%d%%", rain.chance];
        chanceColor = [UIColor whiteColor];
    }
    else {
        chanceText = @"?";
        chanceColor = [UIColor grayColor];
    }
    
    void(^setValues)() = ^{
        _chanceLabel.text = chanceText;
        _chanceLabel.textColor = chanceColor;
        [_mmView setRain:rain animated:NO];
        [_graphLayer setRain:rain animated:animated];
    };

    if (self.alpha == 0 || !animated) {
        setValues();
        return;
    }

    _setting = YES;
    [_dataView popOutThen:^(UIView *view) {
        setValues();
    } popInCompletion:^{
        _setting = NO;
    }];
}


- (void)setLocation:(NSString*)location animated:(BOOL)animated {
    if (self.alpha == 0) {
        _locationLabel.alpha = 1;
        _locationLabel.text = location;
        return;
    }
    
    if (_locationLabel.alpha == 0) {
        _locationLabel.text = location;
        [_locationLabel popInCompletion:nil];
        return;
    }
    
    // dont set if the same
    if ([_locationLabel.text isEqualToString:location])
        return;
    
    [_locationLabel popOutThen:^(UIView *view) {
        _locationLabel.text = location;
    } popInCompletion:nil];
}




@end
