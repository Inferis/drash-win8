//
//  InfoViewController.m
//  Drache
//
//  Created by Tom Adriaenssen on 13/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "InfoViewController.h"
#import <QuartzCore/QuartzCore.h>
#import "IIViewDeckController.h"

@interface InfoViewController () <IIViewDeckControllerDelegate>

@property (nonatomic, strong) IBOutlet UITextView* acknowledgmentsTextView;
@property (nonatomic, strong) IBOutlet UIButton* closeButton;

@end

@implementation InfoViewController

- (id)initWithNibName:(NSString *)nibNameOrNil bundle:(NSBundle *)nibBundleOrNil
{
    self = [super initWithNibName:nibNameOrNil bundle:nibBundleOrNil];
    if (self) {
        // Custom initialization
    }
    return self;
}

- (void)viewDidLoad
{
    [super viewDidLoad];
    
    self.viewDeckController.rightSize = 320;
    if (IsIPad())
        self.closeButton.alpha = 0;
    
    self.acknowledgmentsTextView.text = @"This app uses free weather data provided by Buienradar.nl (see: http://gratisweerdata.buienradar.nl). Incorrect predictions usually are caused by bad data returned from the weather service. Atmospheric conditions can reduce the effectiveness of the predictions.";
}

- (void)viewWillAppear:(BOOL)animated {
    [super viewWillAppear:animated];
    
    NSLog(@"maagd");
    if (!IsIPad()) {
        dispatch_delayed(0.15, ^{
            [[UIApplication sharedApplication] setStatusBarStyle:UIStatusBarStyleDefault animated:YES];
        });
    }
    else {
        self.view.frame = CGRectOffsetLeftAndShrink(self.viewDeckController.view.bounds, self.viewDeckController.view.bounds.size.width-self.viewDeckController.rightSize);
    }
}

- (IBAction)closeTapped:(id)sender {
    [self dismissViewControllerAnimated:YES completion:nil];
}

- (void)willRotateToInterfaceOrientation:(UIInterfaceOrientation)toInterfaceOrientation duration:(NSTimeInterval)duration {
    [super willRotateToInterfaceOrientation:toInterfaceOrientation duration:duration];

    if (IsIPad()) {
        self.view.frame = CGRectOffsetLeftAndShrink(self.viewDeckController.view.bounds, self.viewDeckController.view.bounds.size.width-self.viewDeckController.rightSize);
    }
}

@end
