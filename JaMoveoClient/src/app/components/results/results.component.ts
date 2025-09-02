import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { SongSearchResult } from '../../models/song.model';
import { SignalRService } from '../../services/signalr.service';
import { SongService } from '../../services/song.service';

@Component({
  selector: 'app-results',
  templateUrl: './results.component.html',
  styleUrls: ['./results.component.scss']
})
export class ResultsComponent implements OnInit {

  private readonly signalRService = inject(SignalRService);
  private readonly songService = inject(SongService);
  private readonly router = inject(Router);

  searchResults: SongSearchResult[] = [];
  searchQuery: string = '';

  constructor(

  ) {
    const navigation = this.router.getCurrentNavigation() as any;
    if (navigation?.extras?.state) {
      this.searchResults = navigation.extras.state['results'] || [];
      this.searchQuery = navigation.extras.state['query'] || '';
    }
  }

  ngOnInit(): void {
    if (this.searchResults.length === 0) {
      // Redirect back to main if no results
      this.router.navigate(['/main']);
    }
  }

  async selectSong(song: SongSearchResult): Promise<void> {
    try {
      this.songService.getSong(song).subscribe(async (res) => {
        debugger
        await this.signalRService.selectSong(res.id);
        this.router.navigate(['/live']);
      })

    } catch (error) {
      console.error('Error selecting song:', error);
    }
  }

  goBack(): void {
    this.router.navigate(['/main']);
  }
}
