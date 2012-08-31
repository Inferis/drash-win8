//
//  InfoViewController.m
//  Drache
//
//  Created by Tom Adriaenssen on 13/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "InfoViewController.h"
#import <QuartzCore/QuartzCore.h>

@interface InfoViewController ()

@property (nonatomic, strong) IBOutlet UITextView* acknowledgmentsTextView;

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
    self.contentSizeForViewInPopover = self.view.bounds.size;
    
    self.acknowledgmentsTextView.text = @"Free weather data:\r\n  http://gratisweerdata.buienradar.nl/\r\nCoby by @junkiesxl:\r\n  http://github.com/pjaspers/coby\r\nTin by @junkiesxl:\r\n  http://github.com/pjaspers/tin\r\nAFNetworking by Github:\r\n  http://github.com/AFNetworking/AFNetworking/";
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
    [self dismissViewControllerAnimated:YES completion:nil];
}

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation
{
    return (interfaceOrientation == UIInterfaceOrientationPortrait);
}

@end
