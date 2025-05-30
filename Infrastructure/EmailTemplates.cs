using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public static class EmailTemplates
    {
        public static string getEmailTemplate(EmailTypeEnum emailTypeEnum)
        {

            if (emailTypeEnum == EmailTypeEnum.Welcome)
            {
                return WelcomeEmailTemplate();
            }
            else if (emailTypeEnum == EmailTypeEnum.Reminder)
            {
                return ReminderEmailTemplate();
            }
            else if (emailTypeEnum == EmailTypeEnum.Verify_Email)
            {
                return EmailVerificationTemplate();
            }
            else {
                return "";
            }
        }

        private static string WelcomeEmailTemplate()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome to SwiftLine! Your Queue Management Revolution 🚀</title>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700&display=swap');

        body {
            margin: 0;
            padding: 0;
            font-family: 'Inter', 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333333;
            background-color: #f5f5f5; /* Light grey background */
            -webkit-text-size-adjust: 100%; /* Prevent font size adjustment on mobile */
            -ms-text-size-adjust: 100%;
        }

        /* Essential for consistent rendering across email clients */
        table, td, div {
            box-sizing: border-box;
        }

        .container {
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 8px; /* Softened edges */
            overflow: hidden; /* Ensures border-radius applies to content */
            box-shadow: 0 4px 12px rgba(0,0,0,0.05); /* Subtle shadow */
        }
        .header {
            background-color: #8BA888; /* SwiftLine Sage Green */
            padding: 40px 30px;
            text-align: center;
            color: #ffffff;
        }
        .logo {
            font-size: 32px;
            font-weight: 700;
            color: #ffffff;
            letter-spacing: 1.5px;
            margin-bottom: 5px;
            line-height: 1.2;
        }
        .tagline {
            color: #ffffff;
            font-size: 16px;
            margin-top: 10px;
            font-style: italic;
            opacity: 0.9;
        }
        .content {
            padding: 30px;
            line-height: 1.7; /* Improved readability */
        }
        h1 {
            color: #333333;
            font-size: 28px;
            margin-top: 0;
            margin-bottom: 20px;
            font-weight: 700;
            text-align: center;
        }
        h2 {
            color: #8BA888; /* SwiftLine Sage Green */
            font-size: 22px;
            margin-top: 35px;
            margin-bottom: 20px;
            font-weight: 600;
            text-align: center;
        }
        p {
            margin-bottom: 15px;
            font-size: 15px;
            color: #555555;
        }
        .button-wrapper {
            text-align: center;
            margin: 30px 0;
        }
        .button {
            display: inline-block;
            background-color: #8BA888; /* SwiftLine Sage Green */
            color: #ffffff !important;
            text-decoration: none;
            padding: 15px 30px;
            border-radius: 8px;
            font-weight: 700;
            font-size: 17px;
            letter-spacing: 0.5px;
            transition: background-color 0.3s ease; /* Smooth hover effect */
        }
        .button:hover {
            background-color: #7A9977; /* Darker sage green on hover */
        }
        .features {
            margin: 30px 0;
        }
        .feature {
            display: flex;
            align-items: center; /* Vertically align icon and text */
            margin-bottom: 25px;
            padding-left: 10px;
        }
        .feature-icon-container {
            min-width: 45px;
            height: 45px;
            background-color: #EBF2EB; /* Lighter sage background for icons */
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            margin-right: 20px;
            color: #8BA888; /* Icon color */
            font-size: 24px; /* Larger emoji */
        }
        .feature-title {
            font-weight: 700;
            margin-bottom: 5px;
            color: #333333;
            font-size: 16px;
        }
        .feature-description {
            color: #555555;
            font-size: 14px;
        }
        .steps {
            background-color: #f9f9f9; /* Slightly lighter background for steps section */
            padding: 25px;
            border-radius: 8px;
            margin: 30px 0;
        }
        .step {
            margin-bottom: 18px;
            display: flex;
            align-items: flex-start;
        }
        .step-number {
            min-width: 28px;
            height: 28px;
            background-color: #8BA888; /* SwiftLine Sage Green */
            color: white;
            text-align: center;
            line-height: 28px;
            border-radius: 50%;
            margin-right: 15px;
            font-weight: 600;
            font-size: 15px;
        }
        .step-text {
            flex: 1;
            color: #555555;
            font-size: 15px;
        }
        .footer {
            background-color: #F0F5EF; /* Lightest sage shade for footer */
            padding: 30px;
            text-align: center;
            color: #666666;
            font-size: 13px;
        }
        .social {
            margin: 20px 0;
        }
        .social a {
            display: inline-block;
            margin: 0 10px;
            color: #8BA888; /* SwiftLine Sage Green */
            text-decoration: none;
            font-weight: 600;
            font-size: 14px;
        }
        .footer p {
            margin-bottom: 8px;
            line-height: 1.5;
            color: #777777;
        }
        .footer a {
            color: #8BA888; /* SwiftLine Sage Green */
            text-decoration: none;
            font-weight: 500;
        }
        .footer a:hover {
            text-decoration: underline;
        }

        /* Responsive adjustments for smaller screens */
        @media only screen and (max-width: 620px) {
            .container {
                width: 100% !important;
                border-radius: 0;
            }
            .header, .content, .footer {
                padding: 20px !important;
            }
            h1 {
                font-size: 24px !important;
            }
            h2 {
                font-size: 20px !important;
            }
            .button {
                padding: 12px 25px !important;
                font-size: 16px !important;
            }
            .feature-icon-container {
                min-width: 38px !important;
                height: 38px !important;
                font-size: 20px !important;
                margin-right: 15px !important;
            }
            .feature-title {
                font-size: 15px !important;
            }
            .feature-description {
                font-size: 13px !important;
            }
            .step-number {
                min-width: 26px !important;
                height: 26px !important;
                line-height: 26px !important;
                font-size: 14px !important;
                margin-right: 12px !important;
            }
            .footer, .footer p, .footer a {
                font-size: 12px !important;
            }
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <div class=""logo"">SwiftLine</div>
            <p class=""tagline"">Queue Management, Reimagined.</p>
        </div>

        <div class=""content"">
            <h1>Welcome to a Smarter Way to Queue! 👋</h1>

            <p>Hi {{UserName}},</p>

            <p>We're thrilled to welcome you to SwiftLine, the virtual queue management system that's changing how businesses and organizations handle wait times. Get ready to transform your customer experience!</p>

            <div class=""button-wrapper"">
                <a href=""{{SwiftlineUrl}}"" class=""button"">Set Up Your First Event/Queue</a>
            </div>

            <h2>Why SwiftLine is Your Next Game-Changer</h2>

            <div class=""features"">
                <div class=""feature"">
                    <div class=""feature-icon-container"">📲</div>
                    <div class=""feature-content"">
                        <div class=""feature-title"">Remote Queueing for Customers</div>
                        <div class=""feature-description"">Empower your customers to join lines from anywhere, using their own devices. No more physical waiting!</div>
                    </div>
                </div>

                <div class=""feature"">
                    <div class=""feature-icon-container"">⏱️</div>
                    <div class=""feature-content"">
                        <div class=""feature-title"">Real-Time Updates & Smart Estimates</div>
                        <div class=""feature-description"">Leverage our ML-powered engine for accurate wait times and keep everyone informed with automated status notifications.</div>
                    </div>
                </div>

                <div class=""feature"">
                    <div class=""feature-icon-container"">⚡</div>
                    <div class=""feature-content"">
                        <div class=""feature-title"">Boost Efficiency & Satisfaction</div>
                        <div class=""feature-description"">Reduce physical crowding, optimize staff allocation, and give customers the freedom to manage their time better.</div>
                    </div>
                </div>

                <div class=""feature"">
                    <div class=""feature-icon-container"">📈</div>
                    <div class=""feature-content"">
                        <div class=""feature-title"">Deep Analytics Dashboard</div>
                        <div class=""feature-description"">Gain powerful insights into queue performance, peak times, and customer flow to continually optimize your operations.</div>
                    </div>
                </div>
            </div>

            <div class=""steps"">
                <h2>Ready to Get Started? It's Simple:</h2>
                <div class=""step"">
                    <span class=""step-number"">1</span>
                    <span class=""step-text""><strong>Create Your Event or Service:</strong> Easily set up your first queue and customize it to fit your unique needs.</span>
                </div>

                <div class=""step"">
                    <span class=""step-number"">2</span>
                    <span class=""step-text""><strong>Share Your Queue Link:</strong> Invite attendees to join through a simple link, QR code.</span>
                </div>

                <div class=""step"">
                    <span class=""step-number"">3</span>
                    <span class=""step-text""><strong>Manage Your Queue with Ease:</strong> Utilize your intuitive dashboard to track and manage participants in real-time.</span>
                </div>
            </div>

            <p>Our dedicated support team is here to help you make the most of SwiftLine. If you have any questions or need assistance, don't hesitate to <a href=""#"" style=""color: #8BA888; text-decoration: none; font-weight: bold;"">reach out</a>.</p>

            <p>Say goodbye to long lines and hello to happy customers!</p>

            <p>Best regards,<br>The SwiftLine Team</p>
        </div>

        <div class=""footer"">
            <div class=""social"">
                <a href=""#"">Twitter</a> |
                <a href=""#"">Facebook</a> |
                <a href=""#"">Instagram</a> |
                <a href=""#"">LinkedIn</a>
            </div>
            <p>&copy; 2025 SwiftLine Queue Management. All rights reserved.</p>
            <p>You're receiving this email because you signed up for SwiftLine.<br>
            <a href=""#"" style=""color: #8BA888;"">Unsubscribe</a> | <a href=""#"" style=""color: #8BA888;"">Privacy Policy</a></p>
        </div>
    </div>
</body>
</html>";
        }

  
        private static string ReminderEmailTemplate()
        {
            return @"<!DOCTYPE html>
                        <html>
                        <head>
                            <meta charset=""UTF-8"">
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                            <title>Your Turn is Coming Up Soon - Swiftline</title>
                            <style>
                                /* Base styles */
                                body {
                                    margin: 0;
                                    padding: 0;
                                    font-family: 'Helvetica Neue', Arial, sans-serif;
                                    line-height: 1.6;
                                    color: #333333;
                                    background-color: #f5f5f5;
                                }
        
                                .container {
                                    max-width: 600px;
                                    margin: 0 auto;
                                    background-color: #ffffff;
                                }
        
                                /* Header styles */
                                .header {
                                    background-color: #7D9D74; /* Sage green */
                                    padding: 24px;
                                    text-align: center;
                                }
        
                                .logo {
                                    max-width: 180px;
                                }
        
                                /* Content styles */
                                .content {
                                    padding: 30px;
                                }
        
                                h1 {
                                    color: #333333; /* Black */
                                    font-size: 24px;
                                    margin-top: 0;
                                    margin-bottom: 20px;
                                }
        
                                p {
                                    margin-bottom: 20px;
                                }
        
                                /* Timer styles */
                                .timer-container {
                                    text-align: center;
                                    margin: 25px 0;
                                    padding: 20px;
                                    background-color: #F5F8F4; /* Light sage */
                                    border-radius: 8px;
                                }
        
                                .estimated-time {
                                    font-size: 36px;
                                    font-weight: bold;
                                    color: #7D9D74; /* Sage green */
                                    margin: 10px 0;
                                }
        
                                .time-label {
                                    font-size: 16px;
                                    color: #666666;
                                }
        
                                /* Button styles */
                                .button-container {
                                    text-align: center;
                                    margin: 30px 0;
                                }
        
                                .button {
                                    display: inline-block;
                                    background-color: #7D9D74; /* Sage green */
                                    color: #ffffff; /* White */
                                    text-decoration: none;
                                    padding: 14px 30px;
                                    border-radius: 4px;
                                    font-weight: bold;
                                    letter-spacing: 0.5px;
                                    text-transform: uppercase;
                                    font-size: 16px;
                                }
        
                                /* Urgency note */
                                .urgency-note {
                                    border-left: 4px solid #7D9D74; /* Sage green */
                                    padding: 15px;
                                    margin-bottom: 25px;
                                    background-color: #F5F8F4; /* Light sage */
                                }
        
                                /* Footer styles */
                                .footer {
                                    background-color: #333333; /* Black */
                                    color: #ffffff;
                                    padding: 20px;
                                    text-align: center;
                                    font-size: 12px;
                                }
        
                                .footer-links a {
                                    color: #ffffff;
                                    text-decoration: none;
                                    margin: 0 10px;
                                }
                                 .center {
                                    display: block;
                                    margin-left: auto;
                                    margin-right: auto;
                                    width: 30%;
                                }
                            </style>
                        </head>
                        <body>
                            <div class=""container"">

                                <div>
                                    <img src = ""https://res.cloudinary.com/dddabj5ub/image/upload/v1741908218/swifline_logo_cpsacv.webp"" alt=""Swiftline"" class=""center"">
                                </div>
                                
                                <div class=""content"">
                                    <h1>You're Almost Up!</h1>
            
                                    <p>Hello {{UserName}},</p>
            
                                    <p>Great news! Your turn in the queue is coming up very soon. Please make sure you're ready and stay nearby.</p>
            
                                    <div class=""timer-container"">
                                        <div class=""time-label"">Estimated time until your turn:</div>
                                        <div class=""estimated-time""> {{EstimatedWait}} minutes</div>
                                    </div>
            
                                    <div class=""urgency-note"">
                                        <strong>Important:</strong> To maintain your place in line, please be ready when it's your turn. If you miss your slot, you may need to rejoin the queue.
                                    </div>
            
                                    <p>Check your current status and receive live updates by returning to the app.</p>
            
                                    <div class=""button-container"">
                                        <a href=""{{SwiftlineUrl}}"" class=""button"">CHECK MY STATUS</a>
                                    </div>
            
                                    <p>Thank you for your patience. We'll see you soon!</p>
            
                                    <p>The Swiftline Team</p>
                                </div>
        
                                <!-- Footer -->
                                <div class=""footer"">
                                    <div class=""footer-links"">
                                        <a href=""#"">Help Center</a>
                                        <a href=""#"">Privacy Policy</a>
                                        <a href=""#"">Terms of Service</a>
                                    </div>
            
                                    <p>&copy; 2025 Swiftline. All rights reserved.</p>
                                </div>
                            </div>
                        </body>
                        </html>";


        }

        private static string EmailVerificationTemplate()
        {
            return @"
                  <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset=""UTF-8"">
                        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                      

                        <title>Welcome to Swiftline</title>
                        <style>
                            @import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');
        
                            body {
                                font-family: 'Inter', sans-serif;
                                line-height: 1.6;
                                color: #333;
                                max-width: 600px;
                                margin: 0 auto;
                                padding: 20px;
                                background-color: #f9f9f9;
                            }
                            .logo {
                                text-align: center;
                                margin-bottom: 30px;
                            }
                            .logo img {
                                max-width: 180px;
                            }
                            .container {
                                background-color: white;
                                border-radius: 8px;
                                padding: 30px;
                                border: 1px solid #eaeaea;
                            }
                            h1 {
                                color: #6B8E6E; /* Sage green */
                                margin-top: 0;
                                font-weight: 600;
                            }
                            .button {
                                display: inline-block;
                                background-color: #6B8E6E; /* Sage green */
                                color: white;
                                text-decoration: none;
                                padding: 12px 30px;
                                border-radius: 4px;
                                font-weight: 500;
                                margin: 20px 0;
                            }
                            .expiry-note {
                                color: #6B8E6E; /* Sage green */
                                font-weight: 600;
                                font-size: 14px;
                                border: 1px solid #6B8E6E;
                                display: inline-block;
                                padding: 8px 15px;
                                border-radius: 4px;
                            }
                            .footer {
                                margin-top: 30px;
                                font-size: 12px;
                                color: #666;
                                text-align: center;
                            }
                            .link {
                                color: #6B8E6E; /* Sage green */
                                text-decoration: underline;
                            }
                             .center {
                                display: block;
                                margin-left: auto;
                                margin-right: auto;
                                width: 30%;
                            }
                        </style>
                    </head>
                    <body>
                       
    
                        <div class=""container"">
                            
                            <div>
                                 <img src = ""https://res.cloudinary.com/dddabj5ub/image/upload/v1741908218/swifline_logo_cpsacv.webp"" alt=""Swiftline"" class=""center"">
                            </div>
                            <p>Hello {{UserName}},</p>
        
                            <p>Thank you for Signing up with Swiftline! We're excited to have you on board. To get started with your account, please verify your email address by clicking the button below:</p>
        
                            <div style=""text-align: center;"">
                                <a href=""{{SwiftlineUrl}}"" class=""button"">Verify Email Address</a>
                            </div>
        
                            <p class=""expiry-note"">⏱️ This verification link expires in 1 hour</p>
        
                            <p>If this mail came in your spam folder, the button above wouldn't work. Click on the ""Report as not a spam button"" above to move the mail to your inbox and try to click on the button.
                               OR 
                            you can copy and paste the following link into your browser:</p>
        
                            <p style=""word-break: break-all; font-size: 14px; color: #666;"">{{SwiftlineUrl}}</p>
        
                            <p>Swiftline is designed to help you manage your workflow efficiently and boost your productivity. Once your email is verified, you'll have full access to all features.</p>
                            
                            <p> If you have any questions or need assistance, please don't hesitate to contact our support team at <a href=""mailto:swiftline00@gmail.com"" class=""link"">swiftline00@gmail.com</a>.</p>

                            
                            <p>Best regards,<br>
                            The Swiftline Team</p>
                        </div>
    
                        <div class=""footer"">
                            <p>© 2025 Swiftline. All rights reserved.</p>
                            <p>Visit our website: <a href=""https://www.theswiftline.com/"" class=""link"">https://www.theswiftline.com/</a></p>
                            <p>If you didn't create this account, please ignore this email.</p>
                        </div>
                    </body>
                    </html>";


        }

    }
}
