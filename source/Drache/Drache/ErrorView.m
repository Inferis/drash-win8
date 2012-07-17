//
//  ErrorView.m
//  Drache
//
//  Created by Tom Adriaenssen on 17/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "ErrorView.h"
#import "UIView+Pop.h"

@implementation ErrorView {
    UIImageView* _imageView;
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

    _imageView = [[UIImageView alloc] initWithFrame:(CGRect) { 0, 0, 180, 180 }];
    [self addSubview:_imageView];
}

- (void)layoutSubviews {
    [super layoutSubviews];
    
    _imageView.frame = (CGRect) {
        ceilf((self.bounds.size.width - _imageView.frame.size.width) / 2),
        ceilf((self.bounds.size.height - _imageView.frame.size.height) / 2),
        _imageView.frame.size };
}

- (void)setError:(NSString *)error animated:(BOOL)animated {
    UIImage* image = [UIImage imageNamed:[NSString stringWithFormat:@"%@.png", error]];
    
    // if image is the same, don't bother
    if ([_imageView.image isEqual:image])
        return;
    
    if (animated) {
        [_imageView popOutThen:^(UIView *view) {
            _imageView.image = image;
        } popInCompletion:nil];
    }
    else {
        _imageView.image = image;
    }
}
@end
