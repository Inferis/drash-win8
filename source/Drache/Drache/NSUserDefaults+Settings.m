//
//  NSStandardUserDefaults+Settings.m
//  Drache
//
//  Created by Tom Adriaenssen on 31/08/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "NSUserDefaults+Settings.h"

@implementation NSUserDefaults (Settings)

- (int)entries {
    int entries = [[[NSUserDefaults standardUserDefaults] valueForKey:@"entries"] intValue];
    return MIN(MAX(entries, 6), 24);
}

- (void)setEntries:(int)entries {
    entries = MIN(MAX(entries, 6), 24);
    [[NSUserDefaults standardUserDefaults] setValue:@(entries) forKey:@"entries"];
    [[NSUserDefaults standardUserDefaults] synchronize];
}

@end
