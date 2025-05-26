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
            return "";
            //else if (emailTypeEnum == EmailTypeEnum.Verify_Email)
            //{
            //    return VerifyEmailTemplate();
            //}
        }

        public static string WelcomeEmailTemplate()
        {
            return @"<!DOCTYPE html>
        <html>
        <head>
            <meta charset=""UTF-8"">
            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
            <title>Welcome to SwiftLine Queue Management ⏩</title>
            <style>
                  @import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');

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
                .header {
                    background-color: #8BA888; /* Sage green */
                    padding: 30px;
                    text-align: center;
                    text-color: #ffffff
                }
                .logo {
                    font-size: 28px;
                    font-weight: bold;
                    color: #ffffff;
                    letter-spacing: 1px;
                }
                .tagline {
                    color: #ffffff;
                    margin-top: 10px;
                    font-style: italic;
                }
                .content {
                    padding: 30px;
                    line-height: 1.6;
                }
                h1 {
                    color: #333333;
                    margin-top: 0;
                }
                h2 {
                    color: #8BA888;
                    margin-top: 30px;
                }
                .button {
                    display: inline-block;
                    background-color: #8BA888; /* Sage green */
                    color: #ffffff !important;
                    text-decoration: none;
                    padding: 12px 24px;
                    border-radius: 4px;
                    font-weight: bold;
                    margin: 20px 0;
                }
                .button:hover {
                    background-color: #7A9977;
                }
                .features {
                    margin: 30px 0;
                }
                .feature {
                    display: flex;
                    align-items: flex-start;
                    margin-bottom: 20px;
                }
                .feature-icon {
                    min-width: 30px;
                    height: 30px;
                    background-color: #8BA888;
                    border-radius: 50%;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    margin-right: 15px;
                    color: white;
                    font-weight: bold;
                }
                .feature-content {
                    flex: 1;
                }
                .feature-title {
                    font-weight: bold;
                    margin-bottom: 5px;
                }
                .feature-description {
                    color: #555555;
                }
                .steps {
                    background-color: #f5f5f5;
                    padding: 20px;
                    border-radius: 4px;
                    margin: 30px 0;
                }
                .step {
                    margin-bottom: 15px;
                }
                .step-number {
                    display: inline-block;
                    background-color: #8BA888;
                    color: white;
                    width: 24px;
                    height: 24px;
                    text-align: center;
                    border-radius: 50%;
                    margin-right: 10px;
                }
                .footer {
                    background-color: #f5f5f5;
                    padding: 20px;
                    text-align: center;
                    color: #666666;
                    font-size: 12px;
                }
                .social {
                    margin: 15px 0;
                }
                .social a {
                    display: inline-block;
                    margin: 0 8px;
                    color: #8BA888;
                    text-decoration: none;
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
       
                <div class=""content"">
                    <h1>Welcome to a Smarter Way to Queue!</h1>
            
                    <p>Hi {{UserName}},</p>
            
                    <p>Thank you for choosing Swiftline! We're excited to have you join our community of businesses and organizations that are transforming how people wait in line. <br><br>
                    Your account has been successfully created, and you're now ready to start managing queues more efficiently.</p>
            
                    <a href=""{{SwiftlineUrl}}"" class=""button"">Set Up Your First Queue</a>
            
                    <h2>Why Swiftline Makes Waiting Better</h2>
            
                    <div class=""features"">
                        <div class=""feature"">
                            <div>🔄</div>
                            <div class=""feature-content"">
                                <div class=""feature-title""> Join From Anywhere</div>
                                <div class=""feature-description"">Your customers can join queues remotely from their devices, eliminating the need to physically stand in line.</div>
                            </div>
                        </div>
                
                        <div class=""feature"">
                            <div>⏱️</div>
                            <div class=""feature-content"">
                                <div class=""feature-title""> Real-Time Updates</div>
                                <div class=""feature-description"">Automatic notifications keep everyone informed about queue status, an email reminder is sent when it's almost your turn and estimated wait times.</div>
                            </div>
                        </div>
                
                        <div class=""feature"">
                            <div>✓</div>
                            <div class=""feature-content"">
                                <div class=""feature-title""> Time Efficiency</div>
                                <div class=""feature-description"">Users can multitask and make better use of their time while waiting for their turn.</div>
                            </div>
                        </div>
                
                        <div class=""feature"">
                            <div>📊</div>
                            <div class=""feature-content"">
                                <div class=""feature-title""> Powerful Analytics</div>
                                <div class=""feature-description"">Gain insights into wait times, peak hours, and customer flow to optimize your operations.</div>
                            </div>
                        </div>
                    </div>
            
                    <div class=""steps"">
                        <h2>Get Started in 3 Simple Steps:</h2>
                
                        <div class=""step"">
                            <span class=""step-number"">1</span>
                            <strong>Set up your event or service</strong> - Create your first queue and customize it to fit your needs
                        </div>
                
                        <div class=""step"">
                            <span class=""step-number"">2</span>
                            <strong>Share your queue link</strong> - Invite people to join your queue through email, SMS, or QR code
                        </div>
                
                        <div class=""step"">
                            <span class=""step-number"">3</span>
                            <strong>Start managing your queue</strong> - Use our dashboard to track and manage participants efficiently
                        </div>
                    </div>
            
                    <p>Our support team is available 24/7 to help you get the most out of SwiftLine. If you have any questions or need assistance setting up your queues, don't hesitate to reach out.</p>
            
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
                    <p>© 2025 Swiftline Queue Management. All rights reserved.</p>
                    <p>You're receiving this email because you signed up for Swiftline.<br>
                    <a href=""#"" style=""color: #8BA888;"">Unsubscribe</a> | <a href=""#"" style=""color: #8BA888;"">Privacy Policy</a></p>
                </div>
            </div>
        </body>
        </html>";
        }

    }
}
