//
//  RainData.m
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "RainData.h"
#define NUMPOINTS 7

@implementation RainPoint

- (id)initWithValue:(int)value  {
    if ((self = [self init])) {
        _precipitation = value == 0 ? 0.0 : (CGFloat)pow(10.0, ((double)value - 109.0)/32.0);
        _value = (int)(value * 100.0 / 255.0);
        
        double logistic_intensity = (_value-14)/40.0*12.0;
        _intensity = (int)round(1/(1 + pow(M_E, -logistic_intensity))*100);
        
        _adjustedValue = (int)(MIN(_value, 70)/70.0*100.0);
    }
    
    return self;
}

@end


@implementation RainData {
    NSCalendar* _calendar;
    NSUInteger _components;
}

- (id)init {
    if ((self = [super init])) {
        _calendar = [[NSCalendar alloc] initWithCalendarIdentifier:NSGregorianCalendar];
        _components = NSYearCalendarUnit | NSMonthCalendarUnit | NSDayCalendarUnit | NSHourCalendarUnit | NSMinuteCalendarUnit;
    }
    return self;
}

- (BOOL)parse:(NSString*)data {
    _chance = -1;
    _intensity = 0;
    _precipitation = 0;
    _points = [NSArray array];
    
    if (IsEmpty(data))
        return NO;

    NSArray* lines = [data componentsSeparatedByString:@"\r\n"];
    if (IsEmpty(lines))
        return NO;
    
    
    NSDate* now = [NSDate date];
    NSMutableArray* points = [NSMutableArray array];
    int count = 0;
    for (NSString* line in lines) {
        NSLog(@"%@", line);
        NSArray* parts = [line componentsSeparatedByString:@"|"];
        if (parts.count < 2)
            continue;

        int value = MAX(0, [[parts objectAtIndex:0] intValue]);
//        value = 130 + arc4random() % 50;
//        value = count*(arc4random() % 20) + arc4random() % 40;
//        value = arc4random() % 120;
        NSDate* time = [self scanDate:[parts objectAtIndex:1]];

        if ([time timeIntervalSinceDate:now] > -300) {
            NSLog(@"%@ %d", time, value);
            [points addObject:[[RainPoint alloc] initWithValue:value]];
        }
    }
    
    CGFloat weight = 1;
    int total = -1;
    int totalIntensity = 0;
    CGFloat totalPrecipitation = 0;
    int accounted = 0;
    for (RainPoint* point in points) {
        CGFloat useWeight = point.intensity == 100 ? weight : point.intensity < 2 ? weight*0.1 : weight/2.0;

        totalIntensity = totalIntensity + point.adjustedValue;
        accounted++;
        if (weight > 0) {
            total = MAX(0, total) + (int)(point.intensity*useWeight);
            weight = weight - useWeight;
        }
        totalPrecipitation += point.precipitation/60.0*5.0;

//        NSLog(@"%d -> %fmm/u = %fmm, intensity %d -> %d (%d * %f)", point.value, point.precipitation, point.precipitation/60*5, point.adjustedValue, (int)(point.intensity*useWeight), point.intensity, useWeight);
    }
    
    _chance = MIN(total, 99);
    _intensity = totalIntensity > 0 ? MIN((int)((CGFloat)totalIntensity / (CGFloat)accounted), 100) : 0;
    if (_intensity > 0 || _chance > 0) {
        _chance = MAX(_chance, 1);
        _precipitation = MAX(0.001, totalPrecipitation);
        _intensity = MAX(_intensity, 1);
    }
    else {
        _chance = 0;
        _precipitation = 0;
        _intensity = 0;
    }
    _points = [NSArray arrayWithArray:points];
    
    NSLog(@"d: C=%d%% i=%d p=%f", _chance, _intensity, _precipitation);
    for (RainPoint* p in _points) {
        NSLog(@"p: v=%d av=%d i=%i, p=%f", p.value, p.adjustedValue, p.intensity, p.precipitation);
    }
    return YES;
}

- (NSDate*)scanDate:(NSString*)string {
    NSScanner* scanner = [NSScanner scannerWithString:string];
    int hour, minutes;

    if (![scanner scanInt:&hour])
        return nil;
    [scanner scanString:@":" intoString:nil];
    if (![scanner scanInt:&minutes])
        return nil;
    
    NSDateComponents* components = [_calendar components:_components fromDate:[NSDate date]];
    if (hour < components.hour)
        components.day = components.day + 1;
    components.hour = hour;
    components.minute = minutes;
    
    return [_calendar dateFromComponents:components];
}

+ (RainData*)rainDataFromString:(NSString*)data {
    RainData* result = [RainData new];
    return [result parse:data] ? result : nil;
}

@end

