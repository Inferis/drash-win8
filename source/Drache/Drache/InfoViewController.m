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
    self.viewDeckController.rotationBehavior = IIViewDeckRotationKeepsViewSizes;
    
    if (IsIPad())
        self.closeButton.alpha = 0;
    
    self.acknowledgmentsTextView.text = @"This app uses free weather data provided by Buienradar.nl (see: http://gratisweerdata.buienradar.nl). Incorrect predictions usually are caused by bad data returned from the weather service. Atmospheric conditions can reduce the effectiveness of the predications.";
}

- (BOOL)viewDeckControllerWillOpenRightView:(IIViewDeckController *)viewDeckController animated:(BOOL)animated {
    self.viewDeckController.rightLedge = self.viewDeckController.view.bounds.size.width - 320;
    self.view.frame = CGRectOffsetLeftAndShrink(self.viewDeckController.view.bounds, self.viewDeckController.rightLedge);
    return YES;
}

- (void)viewWillAppear:(BOOL)animated {
    [super viewWillAppear:animated];
    
    if (!IsIPad()) {
        dispatch_delayed(0.15, ^{
            [[UIApplication sharedApplication] setStatusBarStyle:UIStatusBarStyleDefault animated:YES];
        });
    }
    else {
        UIView* popview = [[[self.view superview] superview] superview];
        [popview.subviews objectAtIndex:1];
        CALayer* layer = popview.layer;
        layer.shadowColor = [[UIColor whiteColor] CGColor];
    }
}

- (IBAction)closeTapped:(id)sender {
    if (self.viewDeckController) 
        [self.viewDeckController closeRightView];
    else
        [self dismissViewControllerAnimated:YES completion:nil];
}

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation
{
    return (interfaceOrientation == UIInterfaceOrientationPortrait);
}

@end
