//
//  RainData.m
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "RainData.h"
#import "Coby.h"

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

- (CGFloat)precipitationForEntries:(int)entries {
    CGFloat totalPrecipitation = 0;
    for (RainPoint* point in [_points take:entries]) {
        totalPrecipitation += point.precipitation/60.0*5.0;
    }
    
    if ([self chanceForEntries:entries] > 0)
        totalPrecipitation = MAX(0.001, totalPrecipitation);

    return totalPrecipitation;
}

- (int)intensityForEntries:(int)entries {
    int totalIntensity = 0;
    int accounted = 0;
    for (RainPoint* point in [_points take:entries]) {
        accounted++;
        totalIntensity = totalIntensity + point.adjustedValue;
    }
    
    totalIntensity =  totalIntensity > 0 ? MIN((int)((CGFloat)totalIntensity / (CGFloat)accounted), 100) : 0;
    if ([self chanceForEntries:entries] > 0)
        totalIntensity = MAX(1, totalIntensity);
    return totalIntensity;
}

- (int)chanceForEntries:(int)entries {
    CGFloat weight = 1;
    int chance = -1;
//    NSLog(@"=== chance for %d", entries);
    for (RainPoint* point in [_points take:entries]) {
        CGFloat useWeight = point.intensity == 100 ? weight : point.intensity < 2 ? weight*0.1 : weight/2.0;
        
        if (weight > 0) {
            chance = MAX(0, chance) + (int)(point.intensity*useWeight);
            weight = weight - useWeight;
        }
        else if (point.intensity > 80) {
            CGFloat factor = 1.0f/MAX(1.0f, 100.0f-point.intensity);
            chance = (int)((CGFloat)chance * (1.0f - factor) + (CGFloat)point.intensity * factor);
        }
        
//        NSLog(@"%d -> %fmm/u = %fmm, intensity %d -> %d (%d * %f) ~ %d", point.value, point.precipitation, point.precipitation/60*5, point.adjustedValue, (int)(point.intensity*useWeight), point.intensity, useWeight, chance);
    }
    
    chance = MIN(chance, 99);
    if (chance > 0) {
        chance = MAX(chance, 1);
    }
    else {
        chance = 0;
    }
//    NSLog(@"=== chance for %d = %d", entries, chance);
    
    return chance;
}

- (BOOL)parse:(NSString*)data {
    _points = [NSArray array];
    
    if (IsEmpty(data))
        return NO;

    NSArray* lines = [data componentsSeparatedByString:@"\r\n"];
    if (IsEmpty(lines))
        return NO;
    
    
    NSDate* now = [NSDate date];
    NSMutableArray* points = [NSMutableArray array];
    for (NSString* line in lines) {
        NSArray* parts = [line componentsSeparatedByString:@"|"];
        if (parts.count < 2)
            continue;

        int value = MAX(0, [[parts objectAtIndex:0] intValue]);
//        value = 130 + arc4random() % 50;
//        value = count*(arc4random() % 20) + arc4random() % 40;
//        value = arc4random() % 120;
        NSDate* time = [self scanDate:[parts objectAtIndex:1]];

        if ([time timeIntervalSinceDate:now] > -300) {
            [points addObject:[[RainPoint alloc] initWithValue:value]];
        }
    }
    
    _points = [NSArray arrayWithArray:points];
    
//    NSLog(@"d: C=%d%% i=%d p=%f", _chance, _intensity, _precipitation);
//    for (RainPoint* p in _points) {
//        NSLog(@"p: v=%d av=%d i=%i, p=%f", p.value, p.adjustedValue, p.intensity, p.precipitation);
//    }
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

