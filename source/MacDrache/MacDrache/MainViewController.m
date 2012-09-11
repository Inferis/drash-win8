//
//  MainViewController.m
//  MacDrache
//
//  Created by Tom Adriaenssen on 05/08/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "MainViewController.h"
#import "DataView.h"

@interface MainViewController ()

@property (nonatomic, strong) IBOutlet DataView* dataView;

@end

@implementation MainViewController

- (void)awakeFromNib {
    [super awakeFromNib];
}

- (void)popoverDidShow:(NSNotification *)notification {
    
}

@end
