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

@implementation DataView {
    UILabel* _locationLabel;
    UILabel* _chanceLabel;
    UIView* _mmView;
}

- (id)initWithFrame:(CGRect)frame
{
    self = [super initWithFrame:frame];
    if (self) {
        [self setupViews];
        // Initialization code
    }
    return self;
}

- (void)awakeFromNib {
    [super awakeFromNib];
    [self setupViews];
}

- (void)setupViews {
    // build location label
    _locationLabel = [[UILabel alloc] init];
    _locationLabel.alpha = 0;
    _locationLabel.opaque = NO;
    _locationLabel.textColor = [UIColor whiteColor];
    _locationLabel.backgroundColor = [UIColor clearColor];
    _locationLabel.textAlignment = UITextAlignmentCenter;
    _locationLabel.font = [UIFont fontWithName:@"HelveticaNeue-Light" size:20];
    [self addSubview:_locationLabel];

    _chanceLabel = [[UILabel alloc] init];
    _chanceLabel.alpha = 0;
    _chanceLabel.opaque = NO;
    _chanceLabel.textColor = [UIColor whiteColor];
    _chanceLabel.backgroundColor = [UIColor clearColor];
    _chanceLabel.textAlignment = UITextAlignmentCenter;
    _chanceLabel.font = [UIFont fontWithName:@"HelveticaNeue-UltraLight" size:90];
    [self addSubview:_chanceLabel];
    
    _mmView = [UIView new];
    _mmView.backgroundColor = [UIColor redColor];
    [self addSubview:_mmView];
}

- (void)layoutSubviews {
    [super layoutSubviews];

    CGFloat bottom = self.bounds.size.height > self.bounds.size.width ? 40 : 30;
    CGFloat height = (self.bounds.size.height - bottom) / 3;
    CGFloat width = self.bounds.size.width;
    
    _locationLabel.frame = (CGRect) { 0, 0, width, height };
    _chanceLabel.frame = (CGRect) { 0, height, floorf(width/1.7777777), height };
    CGFloat x = CGRectGetMaxX(_chanceLabel.frame);
    _mmView.frame = (CGRect) { x, height, width-x, height };
}

- (void)setRain:(RainData*)rain animated:(BOOL)animated {
    NSString* chanceText;
    UIColor* chanceColor;

    if (rain && rain.chance > 0) {
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
    };

    if (self.alpha == 0) {
        _chanceLabel.alpha = 1;
        setValues();
        return;
    }

    if (_chanceLabel.alpha == 0) {
        setValues();
        [_chanceLabel popInCompletion:nil];
        return;
    }
    
    [_chanceLabel popOutThen:^(UIView *view) {
        setValues();
    } popInCompletion:nil];
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
    
    [_locationLabel popOutThen:^(UIView *view) {
        _locationLabel.text = location;
    } popInCompletion:nil];
}

- (void)drawRect:(CGRect)rect {
    CGFloat bottom = self.bounds.size.height > self.bounds.size.width ? 40 : 30;
    CGFloat height = (self.bounds.size.height - bottom) / 3;
    
    CGContextRef context = UIGraphicsGetCurrentContext();
    
    // white color
    CGContextSetStrokeColorWithColor(context, [UIColor colorWithWhite:1 alpha:0.8].CGColor);
    // dotted
    CGFloat dash[2] = { 1, 4 };
    CGContextSetLineDash(context, 0, dash, 2);
    // 1px
    CGContextSetLineWidth(context, 1.0);
    
    CGContextMoveToPoint(context, 0, height);
    CGContextAddLineToPoint(context, self.bounds.size.width, height);
    CGContextMoveToPoint(context, 0, height*2);
    CGContextAddLineToPoint(context, self.bounds.size.width, height*2);
    CGContextMoveToPoint(context, 0, height*3);
    CGContextAddLineToPoint(context, self.bounds.size.width, height*3);
    
    // and now draw the Path!
    CGContextStrokePath(context);}


@end
