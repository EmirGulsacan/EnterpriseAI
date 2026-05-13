import { createApplication } from '@angular/platform-browser';
import { createCustomElement } from '@angular/elements';
import { appConfig } from './app/app.config';
import { ChatbotComponent } from './app/shared/components/chatbot/chatbot.component';

createApplication(appConfig)
  .then((appRef) => {
    const chatbotElement = createCustomElement(ChatbotComponent, { injector: appRef.injector });
    customElements.define('enterprise-chatbot', chatbotElement);
  })
  .catch((err) => console.error(err));
