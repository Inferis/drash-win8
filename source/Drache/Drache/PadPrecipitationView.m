//
//  PadPrecipitationView.m
//  Drache
//
//  Created by Tom Adriaenssen on 30/08/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "PadPrecipitationView.h"
#import "UIView+Pop.h"

#define DISABLEDALPHA 0.3

@implementation PadPrecipitationView {
    NSArray* _cloudViews;
    UILabel* _mmLabel;
    int _currentIntensity;
}

- (void)setupViews {
    [super setupViews];
    
    _currentIntensity = 0;
    
    NSMutableArray* clouds = [NSMutableArray arrayWithCapacity:5];
    for (int i=0; i<5; ++i) {
        UIImage* cloudImage = [UIImage imageNamed:[NSString stringWithFormat:@"intensity%d.png", i]];
        UIImageView* cloudView = [[UIImageView alloc] initWithFrame:(CGRect) { 0, 0, cloudImage.size }];
        cloudView.contentMode = UIViewContentModeCenter;
        cloudView.opaque = NO;
        cloudView.alpha = DISABLEDALPHA;
        cloudView.image = cloudImage;
        cloudView.backgroundColor = [UIColor clearColor];
        [self addSubview:cloudView];
        [clouds addObject:cloudView];
    }
    _cloudViews = [NSArray arrayWithArray:clouds];

    _mmLabel = [[UILabel alloc] init];
    _mmLabel.alpha = 1;
    _mmLabel.opaque = NO;
    _mmLabel.textColor = [UIColor whiteColor];
    _mmLabel.backgroundColor = [UIColor clearColor];
    _mmLabel.textAlignment = UITextAlignmentCenter;
    _mmLabel.font = [UIFont fontWithName:@"HelveticaNeue-CondensedBold" size:35];
    _mmLabel.text = @"mm";
    [self addSubview:_mmLabel];
}

- (void)layoutSubviews {
    [super layoutSubviews];
    
    CGSize sz = ((UIView*)[_cloudViews objectAtIndex:0]).bounds.size;
    CGFloat delta = sz.width - 20;
    CGFloat x = floorf((self.bounds.size.width - _cloudViews.count * delta) / 2.0) - 10;
    CGFloat y = floorf((self.bounds.size.height - sz.height) / 2.0) - 20;
    for (int i=0; i<5; ++i) {
        [[_cloudViews objectAtIndex:i] setFrame:(CGRect) { x, y - (i == 0 ? 12 : 0), sz }];
        x += delta;
    }

    UIView* cloud = [_cloudViews objectAtIndex:_currentIntensity];
    sz = [_mmLabel.text sizeWithFont:_mmLabel.font];
    x = CGRectGetMidX(cloud.frame)-floorf(sz.width/2.0f);
    x = MAX(x, CGRectGetMinX([[_cloudViews objectAtIndex:0] frame]));
    CGFloat max = CGRectGetMaxX([[_cloudViews lastObject] frame]);
    x = MIN(x, max-sz.width);
    _mmLabel.frame = (CGRect) { x, CGRectGetMaxY(cloud.frame), sz };
}

- (void)setIntensity:(int)intensity formattedPrecipitation:(NSString*)precipitation animated:(BOOL)animated {
    NSString* text = [precipitation stringByAppendingString:@"mm"];
    UIView* cloud = [_cloudViews objectAtIndex:intensity];
    CGSize sz = [text sizeWithFont:_mmLabel.font];
    CGFloat x = CGRectGetMidX(cloud.frame)-floorf(sz.width/2.0f);
    x = MAX(x, CGRectGetMinX([[_cloudViews objectAtIndex:0] frame]));
    CGFloat max = CGRectGetMaxX([[_cloudViews lastObject] frame]);
    x = MIN(x, max-sz.width);
    CGRect frame = (CGRect) { x, CGRectGetMaxY(cloud.frame), sz };

    if (animated) {
        if (_currentIntensity != intensity) {
            [UIView animateWithDuration:0.15 animations:^{
                [[_cloudViews objectAtIndex:_currentIntensity] setAlpha:DISABLEDALPHA];
            } completion:^(BOOL finished) {
                [UIView animateWithDuration:0.15 animations:^{
                    [cloud setAlpha:1];
                }];
            }];
        }
        
        [_mmLabel popOutThen:^(UIView *view) {
            _mmLabel.text = text;
            _mmLabel.frame = frame;
        } popInCompletion:nil];
    }
    else {
        [[_cloudViews objectAtIndex:_currentIntensity] setAlpha:DISABLEDALPHA];
        [cloud setAlpha:1];
        _mmLabel.text = text;
        _mmLabel.frame = frame;
    }
    
    _currentIntensity = intensity;
}

@end
