//
//  UIView+Pop.m
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "UIView+Pop.h"

@implementation UIView (Pop)

- (void)popInCompletion:(void(^)(void))completion {
    CGAffineTransform transform = self.transform;
    self.transform = CGAffineTransformMakeScale(0.9, 0.9);
    [UIView animateWithDuration:0.30 animations:^{
        self.alpha = 1;
        self.transform = transform;
    } completion:^(BOOL finished) {
    }];
}

- (void)popOutThen:(void(^)(UIView* view))inbetween popInCompletion:(void(^)(void))completion {
    CGAffineTransform transform = self.transform;
    [UIView animateWithDuration:0.15 animations:^{
        self.transform = CGAffineTransformMakeScale(0.9, 0.9);
        self.alpha = 0;
    } completion:^(BOOL finished) {
        if (inbetween) inbetween(self);
        [UIView animateWithDuration:0.15 animations:^{
            self.alpha = 1;
            self.transform = transform;
        } completion:^(BOOL finished) {
            if (completion) completion();
        }];
    }];
}

- (void)popOutCompletion:(void(^)(void))completion {
    CGAffineTransform transform = self.transform;
    [UIView animateWithDuration:0.30 animations:^{
        self.alpha = 0;
        self.transform = CGAffineTransformMakeScale(0.9, 0.9);
    } completion:^(BOOL finished) {
        self.transform = transform;
        if (completion) completion();
    }];
}

@end
