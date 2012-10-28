//
//  PhonePrecipitationView.m
//  Drache
//
//  Created by Tom Adriaenssen on 30/08/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "PhonePrecipitationView.h"
#import "UIView+Pop.h"
#import "NSDate+SolarInfo.h"
#import "NSDate+Extensions.h"
#import <CoreLocation/CoreLocation.h>

@implementation PhonePrecipitationView {
    UIImageView* _cloudView;
    UILabel* _dataLabel;
    UILabel* _mmLabel;
}


- (void)setupViews {
    [super setupViews];
    
    _cloudView = [[UIImageView alloc] initWithFrame:self.bounds];
    _cloudView.contentMode = UIViewContentModeCenter;
    _cloudView.opaque = NO;
    _cloudView.backgroundColor = [UIColor clearColor];
    [self addSubview:_cloudView];
    
    _dataLabel = [[UILabel alloc] init];
    _dataLabel.alpha = 0;
    _dataLabel.opaque = NO;
    _dataLabel.textColor = [UIColor whiteColor];
    _dataLabel.backgroundColor = [UIColor clearColor];
    _dataLabel.textAlignment = UITextAlignmentCenter;
    _dataLabel.font = [UIFont fontWithName:@"HelveticaNeue-UltraLight" size:35];
    _dataLabel.alpha = 0;
    _dataLabel.adjustsFontSizeToFitWidth = YES;
    [self addSubview:_dataLabel];
    
    _mmLabel = [[UILabel alloc] init];
    _mmLabel.alpha = 0;
    _mmLabel.opaque = NO;
    _mmLabel.textColor = [UIColor whiteColor];
    _mmLabel.backgroundColor = [UIColor clearColor];
    _mmLabel.textAlignment = UITextAlignmentCenter;
    _mmLabel.font = [UIFont fontWithName:@"HelveticaNeue-UltraLight" size:35];
    _mmLabel.alpha = 0;
    _mmLabel.text = @"mm";
    [self addSubview:_mmLabel];
    
    UITapGestureRecognizer* tapper = [[UITapGestureRecognizer alloc] initWithTarget:self action:@selector(toggle:)];
    [self addGestureRecognizer:tapper];
}

- (void)layoutSubviews {
    [super layoutSubviews];
    
    CGFloat width = self.bounds.size.width;
    CGFloat height = 28;
    _cloudView.frame = self.bounds;
    _dataLabel.frame = (CGRect) { 0, self.bounds.size.height/2 - height + 1, width, height };
    _mmLabel.frame = (CGRect) { -3, self.bounds.size.height/2 - 1, width, height };
}

- (void)toggle:(UITapGestureRecognizer*)tapper {
    if (self.alpha == 0 || tapper.state != UIGestureRecognizerStateEnded)
        return;
    
    [self popOutThen:^(UIView *view) {
        _cloudView.alpha = 1-_cloudView.alpha;
        _dataLabel.alpha = 1-_cloudView.alpha;
        _mmLabel.alpha = 1-_cloudView.alpha;
    } popInCompletion:nil];
}

- (void)setIntensity:(int)intensity formattedPrecipitation:(NSString*)precipitation animated:(BOOL)animated {
    CLLocationCoordinate2D coord = SharedLocationManager.location.coordinate;
    NSString* night = intensity == 0 && [[NSDate date] isSunSetAtLatitude:coord.latitude longitude:coord.longitude] ? @"n" : @"";
    UIImage* cloudImage = [UIImage imageNamed:[NSString stringWithFormat:@"intensity%d%@.png", intensity, night]];
    
    void(^setValues)() = ^{
        _cloudView.image = cloudImage;
        _dataLabel.text = precipitation;
    };
    
    if (self.alpha == 0 || !animated) {
        setValues();
        return;
    }
    
    [self popOutThen:^(UIView *view) {
        setValues();
    } popInCompletion:nil];
}



@end
