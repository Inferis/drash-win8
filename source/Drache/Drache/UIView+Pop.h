//
//  UIView+Pop.h
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import <UIKit/UIKit.h>

@interface UIView (Pop)

- (void)popInCompletion:(void(^)(void))completion fast:(BOOL)fast;
- (void)popInCompletion:(void(^)(void))completion;
- (void)popOutThen:(void(^)(UIView* view))inbetween popInCompletion:(void(^)(void))completion;
- (void)popOutCompletion:(void(^)(void))completion fast:(BOOL)fast;
- (void)popOutCompletion:(void(^)(void))completion;

@end
