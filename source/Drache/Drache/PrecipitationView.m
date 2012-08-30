//
//  PrecipitationView.m
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "PrecipitationView.h"
#import "PadPrecipitationView.h"
#import "PhonePrecipitationView.h"
#import "UIView+Pop.h"
#import "RainData.h"

@implementation PrecipitationView

- (id)initWithFrame:(CGRect)frame
{
    self = [super initWithFrame:frame];
    if (self) {
        [self setupViews];
        [self setRain:nil animated:NO];
    }
    return self;
}

-(void)awakeFromNib {
    [super awakeFromNib];
    [self setupViews];
    [self setRain:nil animated:NO];
}

- (void)setupViews {
    self.backgroundColor = [UIColor clearColor];
    self.opaque = NO;
}

- (void)setRain:(RainData*)rain animated:(BOOL)animated {
    NSString* mmText;
    
    int intensity = 0;
    CGFloat mm = 0;
    if (rain) {
        mm = floorf(MAX(rain.precipitation, 0)*1000)/1000.0;
        intensity = mm == 0 ? 0 : (int)MAX(1, MIN(1 + (rain.intensity / 25.0), 4));
    }
    
    if (mm > 0 || intensity > 0) {
        NSString* format = mm < 0.01 ? @"%01.3f" : @"%01.2f";
        mmText = floorf(mm) == mm ? [NSString stringWithFormat:@"%d", (int)mm] : [NSString stringWithFormat:format, mm];
    }
    else {
        mmText = @"0";
        intensity = 0;
    }
    
    [self setIntensity:intensity formattedPrecipitation:mmText animated:animated];
}

- (void)setIntensity:(int)intensity formattedPrecipitation:(NSString*)precipitation animated:(BOOL)animated {
}

@end
