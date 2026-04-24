import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Temporary stand-in used by feature routes that haven't been built yet.
 * Each phase that implements a real feature replaces the usage in that
 * feature's routes file.
 */
@Component({
  selector: 'app-placeholder',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="placeholder">
      <h1>{{ title() }}</h1>
      <p>Diese Ansicht wird in einer späteren Projektphase implementiert.</p>
    </section>
  `,
  styles: `
    :host { display: block; padding: 2rem; }
    .placeholder { max-width: 32rem; margin-inline: auto; }
    h1 { margin-top: 0; }
  `
})
export class PlaceholderComponent {
  readonly title = input<string>('Bald verfügbar');
}
