using Domain.Models;

namespace Infrastructure
{
    public static class EmailTemplates
    {

        //private static string imgUrl = "C:\\Users\\ekpok\\source\\repos\\SwitfLine\\Infrastructure\\assets\\theSwiftlineLogo.png";
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
                        <html lang=""en"">
                        <head>
                            <meta charset=""UTF-8"">
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                            <title>Welcome to theswiftline</title>
                            <style>
                                body {
                                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
                                    line-height: 1.6;
                                    margin: 0;
                                    padding: 20px;
                                    background-color: #f5f5f5;
                                }
                                .email-container {
                                    max-width: 600px;
                                    margin: 0 auto;
                                    background: white;
                                    border-radius: 12px;
                                    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
                                    overflow: hidden;
                                }
                                .email-header {
                                    background: linear-gradient(135deg, #6B7D6B 0%, #030712 100%);
                                    color: white;
                                    padding: 40px 30px;
                                    text-align: center;
                                }
                                .logo {
                                    font-size: 28px;
                                    font-weight: bold;
                                    margin-bottom: 10px;
                                    letter-spacing: -0.5px;
                                }
                                .tagline {
                                    font-size: 16px;
                                    opacity: 0.9;
                                    margin: 0;
                                }
                                .email-body {
                                    padding: 40px 30px;
                                }
                                .greeting {
                                    font-size: 20px;
                                    color: #030712;
                                    margin-bottom: 20px;
                                    font-weight: 600;
                                }
                                .content {
                                    color: #4a5568;
                                    font-size: 16px;
                                    margin-bottom: 30px;
                                }
                                .cta-button {
                                    display: inline-block;
                                    background: #6B7D6B;
                                    color: white;
                                    padding: 16px 32px;
                                    text-decoration: none;
                                    border-radius: 8px;
                                    font-weight: 600;
                                    font-size: 16px;
                                    transition: background-color 0.3s ease;
                                    margin: 20px 0;
                                }
                                .cta-button:hover {
                                    background: #5a6b5a;
                                }
                                .info-box {
                                    background: #f8fafc;
                                    border: 1px solid #e2e8f0;
                                    border-radius: 8px;
                                    padding: 20px;
                                    margin: 20px 0;
                                }
                                .info-box strong {
                                    color: #030712;
                                }
                                .footer {
                                    background: #030712;
                                    color: white;
                                    padding: 30px;
                                    text-align: center;
                                    font-size: 14px;
                                }
                                .footer a {
                                    color: #6B7D6B;
                                    text-decoration: none;
                                }
                            </style>
                        </head>
                        <body>

                        <div class=""email-container"">
                            <div class=""email-header"">
                                <div class=""logo"">theswiftline</div>
                                <p class=""tagline"">Your Time, Your Control. Join Any Queue, anywhere.</p>
                            </div>
    
                            <div class=""email-body"">
                                <div class=""greeting"">Welcome to theswiftline, {{UserName}}! 🎉</div>
        
                                <div class=""content"">
                                    <p>We're thrilled to have you join our community of smart queue managers! theswiftline is here to revolutionize how you handle queues and make waiting a thing of the past.</p>
            
                                    <p><strong>What you can do with theswiftline:</strong></p>
                                    <ul>
                                        <li>🏃‍♂️ <strong>Join queues remotely</strong> - No more standing in line</li>
                                        <li>⏱️ <strong>Get real-time updates</strong> - Know exactly when it's your turn</li>
                                        <li>🎮 <strong>Stay entertained</strong> - Play games while you wait</li>
                                        <li>📱 <strong>Receive notifications</strong> - Never miss your turn again</li>
                                        <li>📊 <strong>Create and manage events</strong> - Perfect for organizers</li>
                                    </ul>
                                </div>
        
                                <div style=""text-align: center;"">
                                    <a href=""{{theswiftlineUrl}}"" class=""cta-button"">Start Using theswiftline</a>
                                </div>
        
                                <div class=""info-box"">
                                    <strong>💡 Pro Tip:</strong> Install theswiftline as a PWA on your device for the best experience. Just visit our website and look for the ""Add to Home Screen"" option!
                                </div>
                            </div>
    
                            <div class=""footer"">
                                <p>Ready to skip the line? <a href=""{{theswiftlineUrl}}"">Visit theswiftline</a></p>
                                <p>Questions? We're here to help at support@theswiftline.com</p>
                            </div>
                        </div>

                        </body>
                        </html>";
        }

        private static string ReminderEmailTemplate()
        {
            return @"<!DOCTYPE html>
                        <html lang=""en"">
                        <head>
                            <meta charset=""UTF-8"">
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                            <title>Your Turn is Coming Up - theswiftline</title>
                            <style>
                                body {
                                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
                                    line-height: 1.6;
                                    margin: 0;
                                    padding: 20px;
                                    background-color: #f5f5f5;
                                }
                                .email-container {
                                    max-width: 600px;
                                    margin: 0 auto;
                                    background: white;
                                    border-radius: 12px;
                                    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
                                    overflow: hidden;
                                }
                                .email-header {
                                    background: linear-gradient(135deg, #6B7D6B 0%, #030712 100%);
                                    color: white;
                                    padding: 40px 30px;
                                    text-align: center;
                                }
                                .logo {
                                    font-size: 28px;
                                    font-weight: bold;
                                    margin-bottom: 10px;
                                    letter-spacing: -0.5px;
                                }
                                .tagline {
                                    font-size: 16px;
                                    opacity: 0.9;
                                    margin: 0;
                                }
                                .email-body {
                                    padding: 40px 30px;
                                }
                                .greeting {
                                    font-size: 20px;
                                    color: #030712;
                                    margin-bottom: 20px;
                                    font-weight: 600;
                                }
                                .content {
                                    color: #4a5568;
                                    font-size: 16px;
                                    margin-bottom: 30px;
                                }
                                .cta-button {
                                    display: inline-block;
                                    background: #6B7D6B;
                                    color: white;
                                    padding: 16px 32px;
                                    text-decoration: none;
                                    border-radius: 8px;
                                    font-weight: 600;
                                    font-size: 16px;
                                    transition: background-color 0.3s ease;
                                    margin: 20px 0;
                                }
                                .cta-button:hover {
                                    background: #5a6b5a;
                                }
                                .info-box {
                                    background: #f8fafc;
                                    border: 1px solid #e2e8f0;
                                    border-radius: 8px;
                                    padding: 20px;
                                    margin: 20px 0;
                                }
                                .info-box strong {
                                    color: #030712;
                                }
                                .footer {
                                    background: #030712;
                                    color: white;
                                    padding: 30px;
                                    text-align: center;
                                    font-size: 14px;
                                }
                                .footer a {
                                    color: #6B7D6B;
                                    text-decoration: none;
                                }
                                .divider {
                                    height: 2px;
                                    background: linear-gradient(90deg, #6B7D6B 0%, #030712 100%);
                                    margin: 40px 0;
                                    border: none;
                                }
                            </style>
                        </head>
                        <body>

                        <div class=""email-container"">
                            <div class=""email-header"">
                                <div class=""logo"">theswiftline</div>
                                <p class=""tagline"">Your turn is coming up!</p>
                            </div>
    
                            <div class=""email-body"">
                                <div class=""greeting"">Hey {{UserName}}, it's almost your turn! ⏰</div>
        
                                <div class=""content"">
                                    <p>Great news! You're almost at the front of the queue. Based on our AI-powered predictions, your estimated wait time is:</p>
                                </div>
        
                                <div class=""info-box"" style=""text-align: center; background: linear-gradient(135deg, #6B7D6B20 0%, #03071220 100%); border: 2px solid #6B7D6B;"">
                                    <div style=""font-size: 32px; font-weight: bold; color: #030712; margin-bottom: 10px;"">
                                        {{EstimatedWait}}
                                    </div>
                                    <div style=""font-size: 16px; color: #4a5568;"">
                                        Estimated wait time remaining
                                    </div>
                                </div>
        
                                <div class=""content"">
                                    <p><strong>What to do next:</strong></p>
                                    <ul>
                                        <li>🚗 <strong>Start heading over</strong> if you're not already there</li>
                                        <li>📱 <strong>Keep your notifications on</strong> for real-time updates</li>
                                        <li>👀 <strong>Check the live queue</strong> for any last-minute changes</li>
                                    </ul>
            
                                    <p>Remember, our AI continuously updates wait times based on real queue movement, so times may vary slightly.</p>
                                </div>
        
                                <div style=""text-align: center;"">
                                    <a href=""{{SwiftlineUrl}}"" class=""cta-button"">View Live Queue</a>
                                </div>
        
                                <hr class=""divider"">
        
                                <div class=""content"">
                                    <p style=""font-size: 14px; color: #718096; text-align: center;"">
                                        <strong>Can't make it?</strong> No problem! You can leave the queue anytime through the app, and we'll notify others behind you that they're moving up.
                                    </p>
                                </div>
                            </div>
    
                            <div class=""footer"">
                                <p>Thanks for using theswiftline to save your valuable time!</p>
                                <p>Questions? Reach out at support@theswiftline.com</p>
                            </div>
                        </div>

                        </body>
                        </html>";


        }

        private static string EmailVerificationTemplate()
        {
            return @"<!DOCTYPE html>
                        <html lang=""en"">
                        <head>
                            <meta charset=""UTF-8"">
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                            <title>Verify Your Email - theswiftline</title>
                            <style>
                                body {
                                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
                                    line-height: 1.6;
                                    margin: 0;
                                    padding: 20px;
                                    background-color: #f5f5f5;
                                }
                                .email-container {
                                    max-width: 600px;
                                    margin: 0 auto;
                                    background: white;
                                    border-radius: 12px;
                                    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
                                    overflow: hidden;
                                }
                                .email-header {
                                    background: linear-gradient(135deg, #6B7D6B 0%, #030712 100%);
                                    color: white;
                                    padding: 40px 30px;
                                    text-align: center;
                                }
                                .logo {
                                    font-size: 28px;
                                    font-weight: bold;
                                    margin-bottom: 10px;
                                    letter-spacing: -0.5px;
                                }
                                .tagline {
                                    font-size: 16px;
                                    opacity: 0.9;
                                    margin: 0;
                                }
                                .email-body {
                                    padding: 40px 30px;
                                }
                                .greeting {
                                    font-size: 20px;
                                    color: #030712;
                                    margin-bottom: 20px;
                                    font-weight: 600;
                                }
                                .content {
                                    color: #4a5568;
                                    font-size: 16px;
                                    margin-bottom: 30px;
                                }
                                .cta-button {
                                    display: inline-block;
                                    background: #6B7D6B;
                                    color: white;
                                    padding: 16px 32px;
                                    text-decoration: none;
                                    border-radius: 8px;
                                    font-weight: 600;
                                    font-size: 16px;
                                    transition: background-color 0.3s ease;
                                    margin: 20px 0;
                                    
                                }
                                .cta-button:hover {
                                    background: #5a6b5a;
                                    cursor: pointer;
                                }
                                .info-box {
                                    background: #f8fafc;
                                    border: 1px solid #e2e8f0;
                                    border-radius: 8px;
                                    padding: 20px;
                                    margin: 20px 0;
                                }
                                .info-box strong {
                                    color: #030712;
                                }
                                .footer {
                                    background: #030712;
                                    color: white;
                                    padding: 30px;
                                    text-align: center;
                                    font-size: 14px;
                                }
                                .footer a {
                                    color: #6B7D6B;
                                    text-decoration: none;
                                }
                            </style>
                        </head>
                        <body>

                        <div class=""email-container"">
                            <div class=""email-header"">
                                <div class=""logo"">theswiftline</div>
                                <p class=""tagline"">Almost there!</p>
                            </div>
    
                            <div class=""email-body"">
                                <div class=""greeting"">Verify Your Email, {{UserName}}</div>
        
                                <div class=""content"">
                                    <p>Thanks for signing up with theswiftline! We just need to verify your email address to complete your account setup.</p>
            
                                    <p>Click the button below to verify your email and start skipping the lines:</p>
                                </div>
        
                                <div style=""text-align: center;"">
                                    <a href=""{{theswiftlineUrl}}"" class=""cta-button"">Verify My Email</a>
                                </div>
        
                                <div class=""info-box"">
                                    <strong>⏰ Important:</strong> This verification link will expire at {{ExpirationTime}}. If you didn't create a theswiftline account, you can safely ignore this email.
                                </div>
        
                                <div class=""content"">
                                    <p style=""font-size: 14px; color: #718096;"">
                                        If the button doesn't work, copy and paste this link into your browser:<br>
                                        <span style=""word-break: break-all; color: #6B7D6B;"">{{theswiftlineUrl}}</span>
                                    </p>
                                </div>
                            </div>
    
                            <div class=""footer"">
                                <p>Welcome to the future of queue management!</p>
                                <p>Need help? Contact us at theswiftlinecommunity@gmail.com</p>
                            </div>
                        </div>

                        </body>
                        </html>";


        }

    }
}
