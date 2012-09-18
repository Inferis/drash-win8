//
//  AppDelegate.m
//  MacDrache
//
//  Created by Tom Adriaenssen on 04/08/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "AppDelegate.h"
#import "IIPopoverStatusItem.h"
#import "MainViewController.h"

@implementation AppDelegate {
    IIPopoverStatusItem * _statusItem;
    MainViewController* _mainViewController;
}

@synthesize drashMenu = _drashMenu;

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
    // Insert code here to initialize your application
}


- (void)awakeFromNib {
    _statusItem = [[NSStatusBar systemStatusBar] popoverStatusItemWithImage:[NSImage imageNamed:@"status0"] alternateImage:nil];

    _mainViewController = [[MainViewController alloc] initWithNibName:@"MainView" bundle:nil];

    _statusItem.popover.animates = YES;
    _statusItem.popover.appearance = NSPopoverAppearanceHUD;
    _statusItem.popover.contentViewController = _mainViewController;
    _statusItem.popover.delegate = _mainViewController;
}

@end
