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
#import "PhonePrecipitationView.h"
#import "PadPrecipitationView.h"
#import "GraphLayer.h"
#import <QuartzCore/QuartzCore.h>
#import "DotsLayer.h"
#import "NSUserDefaults+Settings.h"

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
    
    CGFloat locationFontSize = IsIPad() ? 40 : 20;
    CGFloat chanceFontSize = IsIPad() ? 150 : 90;
    
    // build location label
    _locationLabel = [[UILabel alloc] init];
    _locationLabel.alpha = 0;
    _locationLabel.opaque = NO;
    _locationLabel.textColor = [UIColor whiteColor];
    _locationLabel.backgroundColor = [UIColor clearColor];
    _locationLabel.textAlignment = UITextAlignmentCenter;
    _locationLabel.font = [UIFont fontWithName:@"HelveticaNeue-Light" size:locationFontSize];
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
    _chanceLabel.font = [UIFont fontWithName:@"HelveticaNeue-UltraLight" size:chanceFontSize];
    [_dataView addSubview:_chanceLabel];

    _mmView = IsIPad() ? [PadPrecipitationView new] : [PhonePrecipitationView new];
    [_dataView addSubview:_mmView];
    
    _graphLayer = [GraphLayer new];
    [self.layer addSublayer:_graphLayer];
    
    _dotsLayer = [DotsLayer new];
    [self.layer addSublayer:_dotsLayer];
    
    self.clipsToBounds = YES;
    
    [self setRain:nil animated:NO];
}

- (void)layoutSubviews {
    [super layoutSubviews];

    if (_setting)
        return;
    
    CGFloat bottom = self.bounds.size.height > self.bounds.size.width ? 40 : 30;
    CGFloat height = (self.bounds.size.height - bottom) / 3;
    CGFloat width = self.bounds.size.width;
    CGFloat parentWidth = IsIPad() ? 768 : 320;
    CGFloat factor = IsIPad() ? 2.5 : 1.7777777;
    
    _locationLabel.frame = (CGRect) { 0, 0, width, height };
    _dataView.frame = (CGRect) { (width-parentWidth)/2, height, parentWidth, height };
    _chanceLabel.frame = (CGRect) { 10, 0, floorf(parentWidth/factor), height };
    CGFloat x = CGRectGetMaxX(_chanceLabel.frame);
    _mmView.frame = (CGRect) { x, 0, parentWidth-x, height };
    
    _graphLayer.frame = (CGRect) { -1, height*2, width+2, height + bottom+1 };

    _dotsLayer.frame = self.bounds;
    [_dotsLayer setNeedsDisplay];
}

- (void)setRain:(RainData*)rain animated:(BOOL)animated {
    NSString* chanceText = @"?";
    UIColor* chanceColor = [UIColor grayColor];

    if (rain) {
        int entries = [[NSUserDefaults standardUserDefaults] entries];

        int chance = [rain chanceForEntries:entries];
        if (chance >= 0) {
            chanceText = [NSString stringWithFormat:@"%d%%", chance];
            chanceColor = [UIColor whiteColor];
        }
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
    if (IsIPad()) {
        [_chanceLabel popOutThen:^(UIView *view) {
            _chanceLabel.text = chanceText;
            _chanceLabel.textColor = chanceColor;
        } popInCompletion:^{
            _setting = NO;
        }];
        [_mmView setRain:rain animated:animated];
        [_graphLayer setRain:rain animated:animated];
    }
    else {
        [_dataView popOutThen:^(UIView *view) {
            setValues();
        } popInCompletion:^{
            _setting = NO;
        }];
    }
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
