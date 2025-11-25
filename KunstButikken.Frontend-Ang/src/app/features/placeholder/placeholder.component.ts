import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-placeholder-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule],
  template: `
    <section class="placeholder">
      <h1 class="mat-headline-3">{{ titleKey | translate }}</h1>
      <p class="mat-body-1">{{ descriptionKey | translate }}</p>
    </section>
  `,
  styles: [
    `
      .placeholder {
        display: grid;
        gap: 12px;
        align-items: center;
        justify-items: start;
        padding: 64px 0;
        max-width: 640px;

        h1, p {
          margin: 0;
        }
      }
    `
  ]
})
export class PlaceholderComponent {
  private readonly route = inject(ActivatedRoute);

  readonly titleKey = this.route.snapshot.data['titleKey'] ?? 'placeholder.title';
  readonly descriptionKey = this.route.snapshot.data['descriptionKey'] ?? 'placeholder.description';
}
