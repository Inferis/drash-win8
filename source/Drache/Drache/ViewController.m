//
//  ViewController.m
//  Drache
//
//  Created by Tom Adriaenssen on 13/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "ViewController.h"
#import <CoreLocation/CoreLocation.h>
#import "Tin.h"
#import "TinResponse.h"
#import "Coby.h"
#import "InfoViewController.h"

@interface ViewController () <CLLocationManagerDelegate>

@property (nonatomic, strong) IBOutlet UILabel* locationLabel;
@property (nonatomic, strong) IBOutlet UILabel* chanceLabel;
@property (nonatomic, strong) IBOutlet UILabel* lastUpdateLabel;
@property (nonatomic, strong) IBOutlet UIView* dataView;
@property (nonatomic, strong) IBOutlet UIActivityIndicatorView* smallSpinner;
@property (nonatomic, strong) IBOutlet UIImageView* errorImageView;

@end

@implementation ViewController {
    CLLocationManager* _locationManager;
    CLLocation* _location;
    CLGeocoder* _geocoder;
    BOOL _fetchingRain;
    NSTimer* _timer;
    int _operations;
    BOOL _infoPresenting;
}

- (void)viewDidLoad
{
    [super viewDidLoad];
    self.errorImageView.alpha = 0;
    self.dataView.alpha = 0;
    self.smallSpinner.alpha = 0;
    self.chanceLabel.text = @"";
    self.locationLabel.text = @"(resolving...)";
    
    UILongPressGestureRecognizer* longTapper = [[UILongPressGestureRecognizer alloc] initWithTarget:self action:@selector(forcedRefresh:)];
    longTapper.minimumPressDuration = 1.5;
    [self.view addGestureRecognizer:longTapper];

    _locationManager = [CLLocationManager new];
    _locationManager.delegate = self;
    _locationManager.distanceFilter = 500;
    _locationManager.desiredAccuracy = kCLLocationAccuracyHundredMeters;
    [_locationManager startUpdatingLocation];
    _location = _locationManager.location;
    
    _geocoder = [CLGeocoder new];
    
    [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(reachabilityChanged:) name:kReachabilityChangedNotification object:nil];
}

- (void)viewWillAppear:(BOOL)animated {
    [super viewWillAppear:animated];
    
    [[UIApplication sharedApplication] setStatusBarStyle:UIStatusBarStyleBlackOpaque animated:YES];
    
    _operations = 0;
    [self endOperation];
}

- (void)viewDidAppear:(BOOL)animated {
    [super viewDidAppear:animated];
    
    if (!_infoPresenting)
        [self updateWeather];
    _infoPresenting = NO;
}

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation
{
    return (interfaceOrientation != UIInterfaceOrientationPortraitUpsideDown);
}

- (void)forcedRefresh:(UILongPressGestureRecognizer*)longTapper {
    if (longTapper.state == UIGestureRecognizerStateBegan)
        [self updateWeather];
}

- (IBAction)infoTapped:(id)sender {
    InfoViewController* infoViewController = [[InfoViewController alloc] initWithNibName:nil bundle:nil];
    infoViewController.modalTransitionStyle = UIModalTransitionStyleFlipHorizontal;
    [self presentViewController:infoViewController animated:YES completion:^{
    }];
    _infoPresenting = YES;
}

#pragma mark - network

- (void)reachabilityChanged:(NSNotification*)notification {
    [self updateWeather];
}

#pragma mark - Location Manager Delegate

- (void)locationManager:(CLLocationManager *)manager didChangeAuthorizationStatus:(CLAuthorizationStatus)status {
    _location = manager.location;
    [self updateWeather];
}

- (void)locationManager:(CLLocationManager *)manager didUpdateToLocation:(CLLocation *)newLocation fromLocation:(CLLocation *)oldLocation {
    _location = newLocation;
    [self updateWeather];
    if (!_location)
        return;

    [self startOperation];
    [_geocoder reverseGeocodeLocation:_location completionHandler:^(NSArray *placemarks, NSError *error) {
        [self endOperation];

        if (!_location) {
            [self updateWeather];
            return;
        }
        
        NSString* location = [NSString stringWithFormat:@"%0.5f,%0.5f", _location.coordinate.latitude, _location.coordinate.longitude];
        if (!IsEmpty(placemarks)) {
            CLPlacemark* mark = [placemarks first];
            if (!IsEmpty(mark.locality)) {
                if (!IsEmpty(mark.country)) {
                    location = [NSString stringWithFormat:@"%@, %@", mark.locality, mark.country];
                }
                else
                    location = mark.locality;
            }
        }

        [self visualizeLocation:location];
    }];
}

- (void)updateWeather {
    [_timer invalidate];
    _timer = nil;

    if (![Drache.network isReachable]) {
        [self visualizeNoNetwork];
        return;
    }
    
    if (!_location) {
        [self visualizeNoLocationServices];
        return;
    }
    
    [self fetchRain];
}

- (void)visualizeNoNetwork {
    [self visualizeErrorWithImage:[UIImage imageNamed:@"nonetwork.png"]];
}

- (void)visualizeNoLocationServices {
    [self visualizeErrorWithImage:[UIImage imageNamed:@"nolocation.png"]];
}

- (void)visualizeFetchLocation {
    [self startOperation];
}


- (void)visualizeErrorWithImage:(UIImage*)errorImage {
    if (self.dataView.alpha != 0) {
        [UIView animateWithDuration:0.30 animations:^{
            self.dataView.alpha = 0;
        } completion:^(BOOL finished) {
            [self visualizeErrorWithImage:errorImage];
        }];
    }
    else if (![self.errorImageView.image isEqual:errorImage]) {
        [UIView animateWithDuration:0.15 animations:^{
            self.errorImageView.alpha = 0;
        } completion:^(BOOL finished) {
            self.errorImageView.image = errorImage;
            [UIView animateWithDuration:0.15 animations:^{
                self.errorImageView.alpha = 1;
            }];
        }];
    }
}

- (void)visualizeLocation:(NSString*)location {
    NSLog(@"visualize location %@", location);

    if (self.errorImageView.alpha > 0) {
        [UIView animateWithDuration:0.15 animations:^{
            self.errorImageView.alpha = 0;
        } completion:^(BOOL finished) {
            [self visualizeLocation:location];
        }];
        return;
    }
    
    if (self.dataView.alpha == 0) {
        self.locationLabel.text = location;
        [UIView animateWithDuration:0.15 animations:^{
            self.dataView.alpha = 1;
        } completion:^(BOOL finished) {
        }];
        return;
    }

    if ([location isEqualToString:self.locationLabel.text])
        return;

    if (self.locationLabel.alpha != 0) {
        [UIView animateWithDuration:0.075 animations:^{
            self.locationLabel.alpha = 0;
        } completion:^(BOOL finished) {
            self.locationLabel.text = location;
            [UIView animateWithDuration:0.075 animations:^{
                self.locationLabel.alpha = 1;
            }];
        }];
        return;
    }

    self.locationLabel.text = location;
    [UIView animateWithDuration:0.15 animations:^{
        self.locationLabel.alpha = 1;
    }];
}

- (void)fetchRain {
    [_timer invalidate];
    _timer = nil;
    
    if (_fetchingRain)
        return;
    
    [self startOperation];
    NSString* query = [NSString stringWithFormat:@"lat=%f&lon=%f",
                       _locationManager.location.coordinate.latitude,
                       _locationManager.location.coordinate.longitude];
    _fetchingRain = YES;
    [Tin get:@"http://gps.buienradar.nl/getrr.php" query:query success:^(TinResponse *response) {
        [self endOperation];

        int total = -1;
        if (!response.error) {
            NSArray* lines = [response.bodyString componentsSeparatedByCharactersInSet:[NSCharacterSet characterSetWithCharactersInString:@"\n"]];
            if (!IsEmpty(lines)) {
                CGFloat weight = 1;
                int count = 6;
                for (NSString* line in lines) {
                    if (line.length < 4) continue;
                    
                    int value = MAX(0, [[line substringToIndex:4] intValue]);
                    value = (int)(value * 100.0 / 255.0);
                    //value = arc4random() % 50;

                    double intensity = (value-14)/40.0*12.0;
                    int logistic_intensity = (int)round(1/(1 + pow(M_E, -intensity))*100);

                    CGFloat useWeight = logistic_intensity == 100 ? weight : weight/2.0;
                    total = MAX(0, total) + (int)(logistic_intensity*useWeight);
                    weight = weight - useWeight;
                    
                    NSLog(@"value = %d -> intensity %f -> %d * weight = %f -> %d", value, intensity, logistic_intensity, weight, (int)(logistic_intensity*useWeight));

                    if (weight <= 0)
                        break;
                    if (--count <= 0)
                        break;
                }
            }
            
        }
        
        _fetchingRain = NO;
        _timer = [NSTimer scheduledTimerWithTimeInterval:5*60 target:self selector:@selector(updateWeather) userInfo:nil repeats:NO];
        [self visualizeChance:MIN(total, 99)];
    }];
}

- (void)visualizeChance:(int)chance {
    NSLog(@"setting chance = %d", chance);
    
    if (self.errorImageView.alpha > 0) {
        [UIView animateWithDuration:0.15 animations:^{
            self.errorImageView.alpha = 0;
        } completion:^(BOOL finished) {
            [self visualizeChance:chance];
        }];
        return;
    }

    NSString* chanceText;
    UIColor* chanceColor;
    if (chance < 0) {
        chanceText = @"?";
        chanceColor = [UIColor grayColor];
    }
    else {
        chanceText = [NSString stringWithFormat:@"%d%%", chance];
        chanceColor = [UIColor whiteColor];
    }
    
    if (self.dataView.alpha == 0) {
        self.chanceLabel.alpha = 1;
        self.chanceLabel.textColor = chanceColor;
        self.chanceLabel.text = chanceText;
        [UIView animateWithDuration:0.30 animations:^{
            self.dataView.alpha = 1;
        }];
    }
    else if (self.chanceLabel.alpha != 0) {
        [UIView animateWithDuration:0.15 animations:^{
            self.chanceLabel.alpha = 0;
        } completion:^(BOOL finished) {
            self.chanceLabel.textColor = chanceColor;
            self.chanceLabel.text = chanceText;
            [UIView animateWithDuration:0.15 animations:^{
                self.chanceLabel.alpha = 1;
            }];
        }];
    }
    else {
        self.chanceLabel.text = chanceText;
        [UIView animateWithDuration:0.3 animations:^{
            self.chanceLabel.alpha = 1;
        }];
    }
}

- (void)startOperation {
    if (_operations++)
        return;

    [UIView animateWithDuration:0.3 animations:^{
        [self.smallSpinner startAnimating];
        self.smallSpinner.alpha = 1;
    }];
}

- (void)endOperation {
    if (--_operations > 0)
        return;
    
    _operations = 0;

    [UIView animateWithDuration:0.3 animations:^{
        self.smallSpinner.alpha = 0;
        if (IsEmpty(self.chanceLabel.text)) {
            self.dataView.alpha = 0;
        }
    } completion:^(BOOL finished) {
        [self.smallSpinner stopAnimating];
    }];
}

@end
