//
//  RainData.m
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "RainData.h"
#define NUMPOINTS 7

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
        NSArray* parts = [line componentsSeparatedByString:@"|"];
        if (parts.count < 2)
            continue;

        int value = MAX(0, [[parts objectAtIndex:0] intValue]);
        NSDate* time = [self scanDate:[parts objectAtIndex:1]];

        if ([time timeIntervalSinceDate:now] > -300) {
            [points addObject:[NSNumber numberWithInt:value]];
        }

        if (count++ > NUMPOINTS)
            break;
    }
    
    CGFloat weight = 1;
    int total = -1;
    int totalIntensity = 0;
    CGFloat totalmm;
    int accounted = 0;
    for (NSNumber* pointValue in points) {
        int value = [pointValue intValue];

        CGFloat mm = value == 0 ? 0.0 : (CGFloat)pow(10.0, ((double)value - 109.0)/32.0);
        value = (int)(value * 100.0 / 255.0);
        //value = arc4random() % 75;
        
        double intensity = (value-14)/40.0*12.0;
        int logistic_intensity = (int)round(1/(1 + pow(M_E, -intensity))*100);
        
        CGFloat useWeight = logistic_intensity == 100 ? weight : weight/2.0;
        
        int intensityAdjustedValue = (int)(MIN(value, 70)/70.0*100.0);

        totalIntensity = totalIntensity + (int)(intensityAdjustedValue*useWeight);
        accounted++;
        total = MAX(0, total) + (int)(logistic_intensity*useWeight);
        weight = weight - useWeight;
        totalmm += mm;

        NSLog(@"%d -> %fmm, intensity %f -> %d (%d * %f)", value, mm, intensity, (int)(logistic_intensity*useWeight), logistic_intensity, weight);
        
        if (weight <= 0)
            break;
    }
    
    _chance = MIN(total, 99);
    _intensity = totalIntensity > 0 ? MIN(1 + (int)((CGFloat)totalIntensity / (CGFloat)accounted / 100), 100) : 0;
    _precipitation = totalmm;
    _points = [NSArray arrayWithArray:points];
    
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
