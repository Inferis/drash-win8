//
//  PrecipitationView.m
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "PrecipitationView.h"
#import "UIView+Pop.h"
#import "RainData.h"

@implementation PrecipitationView {
    UIImageView* _cloudView;
    UILabel* _dataLabel;
    UILabel* _mmLabel;
}


- (id)initWithFrame:(CGRect)frame
{
    self = [super initWithFrame:frame];
    if (self) {
        [self setupViews];
    }
    return self;
}

-(void)awakeFromNib {
    [super awakeFromNib];
    [self setupViews];
}

- (void)setupViews {
    self.backgroundColor = [UIColor clearColor];
    self.opaque = NO;
    
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
    
    [self setRain:nil animated:NO];
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

- (void)setRain:(RainData*)rain animated:(BOOL)animated {
    NSString* mmText;
    UIImage* cloudImage = [UIImage imageNamed:[NSString stringWithFormat:@"intensity%d.png", MIN(MAX(0, rain.intensity), 4)]];

    if (rain) {
        CGFloat mm = MAX(rain.precipitation, 0);
        mmText = floorf(mm) == mm ? [NSString stringWithFormat:@"%d", (int)mm] : [NSString stringWithFormat:@"%01.2f", mm];
    }
    else {
        mmText = @"0";
    }
    
    void(^setValues)() = ^{
        _cloudView.image = cloudImage;
        _dataLabel.text = mmText;
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
