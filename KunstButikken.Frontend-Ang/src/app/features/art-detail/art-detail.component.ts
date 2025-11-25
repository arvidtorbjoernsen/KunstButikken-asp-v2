import { CommonModule } from '@angular/common';
import { Component, OnDestroy } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

import { UiArt } from '../../core/models/art.model';
import { ArtService } from '../../core/services/art.service';
import { TranslationService } from '../../core/services/translation.service';

@Component({
  selector: 'app-art-detail',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, RouterModule, TranslateModule],
  templateUrl: './art-detail.component.html',
  styleUrls: ['./art-detail.component.scss']
})
export class ArtDetailComponent implements OnDestroy {
  art: UiArt | undefined;
  private subs = new Subscription();

  // Return the active locale string by calling the translation service signal.
  // Use a method so we don't reference the injected service during field initialization.
  locale(): string {
    return this.translationService.locale();
  }

  constructor(
    private readonly route: ActivatedRoute,
    private readonly artService: ArtService,
    private readonly translationService: TranslationService
  ) {
    const sub = this.route.paramMap.subscribe((params) => {
      const id = params.get('id') ?? undefined;
      if (id) {
        this.load(id);
      }
    });
    this.subs.add(sub);
  }

  private load(id: string): void {
    const s = this.artService.getById(id).subscribe((a) => {
      this.art = a ?? undefined;
    });
    this.subs.add(s);
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }
}
