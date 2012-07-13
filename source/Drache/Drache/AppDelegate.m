//
//  AppDelegate.m
//  Drache
//
//  Created by Tom Adriaenssen on 13/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "AppDelegate.h"
#import "ViewController.h"
#import "TestFlight.h"

@implementation AppDelegate


- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions
{
#if !DEBUG
    [TestFlight takeOff:@"95c7c2a7e1a1929c69c4f452f3b85108_MjQyNjIwMTItMDctMDMgMTc6MzU6NTQuNTc0NzYw"];
#endif

    _network = [Reachability reachabilityForInternetConnection];
    [_network startNotifier];

    self.window = [[UIWindow alloc] initWithFrame:[[UIScreen mainScreen] bounds]];

    // Override point for customization after application launch.
    self.viewController = [[ViewController alloc] initWithNibName:@"ViewController" bundle:nil];
    self.window.rootViewController = self.viewController;
    [self.window makeKeyAndVisible];
    
    
    for (int i=0; i<100; i++) {
        double i6 = (i-14)/40.0*16.0;
        NSLog(@"%d => %d", i, (int)round(1/(1 + pow(M_E, -i6))*100));
    }
    
    return YES;
}

- (void)applicationWillResignActive:(UIApplication *)application
{
    // Sent when the application is about to move from active to inactive state. This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) or when the user quits the application and it begins the transition to the background state.
    // Use this method to pause ongoing tasks, disable timers, and throttle down OpenGL ES frame rates. Games should use this method to pause the game.
    [_network stopNotifier];
}

- (void)applicationDidEnterBackground:(UIApplication *)application
{
    // Use this method to release shared resources, save user data, invalidate timers, and store enough application state information to restore your application to its current state in case it is terminated later. 
    // If your application supports background execution, this method is called instead of applicationWillTerminate: when the user quits.
}

- (void)applicationWillEnterForeground:(UIApplication *)application
{
    // Called as part of the transition from the background to the inactive state; here you can undo many of the changes made on entering the background.
}

- (void)applicationDidBecomeActive:(UIApplication *)application
{
    // Restart any tasks that were paused (or not yet started) while the application was inactive. If the application was previously in the background, optionally refresh the user interface.
    [_network startNotifier];
}

- (void)applicationWillTerminate:(UIApplication *)application
{
    // Called when the application is about to terminate. Save data if appropriate. See also applicationDidEnterBackground:.
}

@end
