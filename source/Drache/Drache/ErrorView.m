//
//  ErrorView.m
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "ErrorView.h"

@implementation ErrorView {
    UIImageView* _imageView;
}

- (id)initWithFrame:(CGRect)frame
{
    self = [super initWithFrame:frame];
    if (self) {
        _imageView = [[UIImageView alloc] initWithFrame:(CGRect) { 0, 0, 180, 180 }];
    }
    return self;
}

- (void)layoutSubviews {
    [super layoutSubviews];
    
    _imageView.frame = (CGRect) {
        ceilf((self.bounds.size.width - _imageView.frame.size.width) / 2),
        ceilf((self.bounds.size.height - _imageView.frame.size.height) / 2),
        _imageView.frame.size };
}

@end
